using FreeRP.Database;
using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace FreeRP.Helpers.Database
{
    public struct FrpLinqExpressionVisitor
    {
        private readonly List<FrpQuery> _queries = [];
        private readonly Expression _expr;
        private static readonly CultureInfo _ci = CultureInfo.GetCultureInfo("en-US");
        private bool _findBool = false;

        public FrpLinqExpressionVisitor(Expression expr)
        {
            if (expr is LambdaExpression lambda)
            {
                _expr = lambda.Body;
            }
            else
            {
                throw new NotSupportedException($"Expression {expr} must be a lambda expression");
            }
        }

        public List<FrpQuery> Resolve()
        {
            Visit(_expr);

            if (_findBool)
            {
                FixBool();
            }

            return _queries;
        }

        /// <summary>
        /// only 'x => x.Bool' fix to x => x.Bool == true
        /// only 'x => x.Foo.Bar.Contains('a')' fix to x => x.Foo.Bar.Contains('a') == true
        /// only 'x => x.Foo.StartsWith("foo")' fix to x => x.Foo.StartsWith("foo") == true
        /// only 'x => x.Foo.EndsWith("bar")' fix to x => x.Foo.EndsWith("bar") == true
        /// only 'x => 'string.IsNullOrEmpty(x.Foo)' fix to x => string.IsNullOrEmpty(x.Foo) == true
        /// only 'x => x.Foo.Equals("bar")' fix to x => x.Foo.Equals("bar") == true
        /// </summary>
        private readonly void FixBool()
        {
            int i = 0;
            List<FrpQuery> queries = _queries;
            FrpQuery query;

            void Check()
            {
                bool find = false;
                int lastI = i - 1;
                int nextI = i + 1;

                if (nextI < queries.Count)
                {
                    var nq = queries[nextI];
                    if (nq.ValueType == FrpQueryType.ValueBoolean && (query.Next == FrpQueryType.QueryEqual || query.Next == FrpQueryType.QueryNotEqual))
                    {
                        find = true;
                    }
                }

                if (find == false && lastI >= 0)
                {
                    var lq = queries[lastI];
                    if (lq.ValueType == FrpQueryType.ValueBoolean && (lq.Next == FrpQueryType.QueryEqual || lq.Next == FrpQueryType.QueryNotEqual))
                    {
                        find = true;
                    }
                }

                if (find == false)
                {
                    queries.Insert(i + 1, new() { Next = query.Next, ValueType = FrpQueryType.ValueBoolean, Value = GetValueAsString(true) });
                    query.Next = FrpQueryType.QueryEqual;
                }
            }

            for (; i < _queries.Count; i++)
            {
                query = _queries[i];
                if (query.MemberType == FrpQueryType.ValueBoolean)
                {
                    Check();
                }
                else
                {
                    switch (query.CallType)
                    {
                        case FrpQueryType.CallContains:
                        case FrpQueryType.CallStartWith:
                        case FrpQueryType.CallEndsWith:
                        case FrpQueryType.CallEquals:
                        case FrpQueryType.CallIsNullOrEmpty:
                            Check();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void Visit(Expression ex)
        {
            // x => x.Foo '==' 0
            if (ex is BinaryExpression bin)
            {
                //x.IntArray[1]
                if (ex.NodeType == ExpressionType.ArrayIndex)
                {
                    CallArgsToQuery(bin.Left, bin.Right, FrpQueryType.CallArrayIndex);
                    return;
                }

                VisitBinary(bin);
                return;
            }
            // x => x.'Foo' == 0
            else if (ex is MemberExpression mem)
            {
                VisitMember(mem);
                return;
            }
            // x => x.Foo == '0'
            else if (ex is ConstantExpression co)
            {
                VisitConstant(co);
                return;
            }
            // x => x.Foo.'StartsWith'("bar")
            else if (ex is MethodCallExpression call)
            {
                VisitCall(call);
                return;
            }
            //x => x.'IntArray.Length' == 5
            //x => !x.Bool
            else if (ex is UnaryExpression unary)
            {
                VisitUnray(unary);
                return;
            }

            throw new NotSupportedException($"{nameof(Expression)} is not supported.");
        }

        /// <summary>
        /// x => x.Foo '==' Bar or
        /// !=, &&, ||, +, -, /, *, >, <, >=, <=
        /// </summary>
        /// <param name="bin"></param>
        private void VisitBinary(BinaryExpression bin)
        {
            Visit(bin.Left);
            _queries.Last().Next = GetOperator(bin.NodeType);
            Visit(bin.Right);
        }

        /// <summary>
        /// MemberExpression: x => x.'Foo' == 'Bar.Foo'
        /// ParameterExpression: x => 'x.Foo' == Bar.Foo
        /// ConstantExpression: x => x.Foo == 'Bar.Foo'
        /// Special member access:
        /// List: x => x.IntList.'Count' == 5
        /// String: x => x.Foo.'Lenght' == 5
        /// </summary>
        /// <param name="node"></param>
        private void VisitMember(MemberExpression node)
        {
            var member = node.Member;
            if (member.DeclaringType is not null && node.Expression is not null)
            {
                //List: x => x.IntList.'Count' == 5
                //String: x => x.Foo.'Lenght' == 5
                if (
                    (IsCollection(member.DeclaringType) && member.Name == "Count") ||
                    (member.DeclaringType == typeof(String) && member.Name == "Length"))
                {
                    FrpQuery qs = new();
                    GetQueryFromParameterOrConstantExpression(node.Expression, qs);
                    qs.CallType = FrpQueryType.CallCount;
                    _queries.Add(qs);
                    return;
                }
                else if (member.DeclaringType == typeof(DateTime))
                {
                    throw new NotSupportedException($"{nameof(DateTime)} is not supported. Please use {nameof(FrpUtcDateTime)}");
                }
            }

            FrpQuery query = new();
            GetQueryFromParameterOrConstantExpression(node, query);
            _queries.Add(query);
            return;
        }

        /// <summary>
        /// x => x.Foo == '0'
        /// </summary>
        /// <param name="node"></param>
        private void VisitConstant(ConstantExpression node)
        {
            FrpQuery query = new();
            GetQueryFromParameterOrConstantExpression(node, query);
            _queries.Add(query);
            return;
        }

        /// <summary>
        /// x => x.'IntArray.Length' == 5
        /// x => !x.Bool
        /// </summary>
        /// <param name="unray"></param>
        private void VisitUnray(UnaryExpression unray)
        {
            //x => x.'IntArray.Length' == 5
            if (unray.NodeType == ExpressionType.ArrayLength)
            {
                Visit(unray.Operand);
                _queries.Last().CallType = FrpQueryType.CallCount;
                return;
            }

            //x => !x.Bool
            //change to x => x.Bool == false
            if (unray.NodeType == ExpressionType.Not)
            {
                Visit(unray.Operand);
                _queries.Last().Next = FrpQueryType.QueryEqual;
                _queries.Add(new FrpQuery() { Value = GetValueAsString(false), ValueType = FrpQueryType.ValueBoolean });
                return;
            }
        }

        #region Calls

        /// <summary>
        /// x => x.Foo.'StartWith()'
        /// </summary>
        /// <param name="call"></param>
        private void VisitCall(MethodCallExpression call)
        {
            var ct = GetCallType(call.Method.Name);

            switch (ct)
            {
                case FrpQueryType.CallToLower:
                case FrpQueryType.CallToUpper:
                    VisitCallToLowerOrToUpper(call, ct); return;
                case FrpQueryType.CallContains:
                    VisitCallContains(call, ct); return;
                case FrpQueryType.CallStartWith:
                case FrpQueryType.CallEndsWith:
                    VisitCallStartsWithOrEndsWith(call, ct); return;
                case FrpQueryType.CallIndexOf:
                    VisitCallIndexOf(call, ct); return;
                case FrpQueryType.CallArrayIndex:
                    VisitCallArrayIndex(call, ct); return;
                case FrpQueryType.CallIsNullOrEmpty:
                    VisitCallIsNullOrEmpty(call, ct); return;
                case FrpQueryType.CallEquals:
                    VisitCallEquals(call, ct); return;
                case FrpQueryType.CallCount:
                    VisitCallCount(call, ct); return;
                default:
                    break;
            }

            throw new NotSupportedException($"Not supported method call {call.Method.Name}");
        }

        /// <summary>
        /// x => x.Foo.'ToLower()' == "foo"
        /// x => x.Foo.'ToUpper()' == "BAR"
        /// </summary>
        /// <param name="call"></param>
        private void VisitCallToLowerOrToUpper(MethodCallExpression call, FrpQueryType ct)
        {
            if (call.Object is not null)
            {
                Visit(call.Object);
                _queries.Last().CallType = ct;
                return;
            }

            throw new NotSupportedException($"Not supported method call {call.Method.Name}");
        }

        /// <summary>
        /// String.Contains: x => x.'Foo.Bar.Contains('a')' == true
        /// List.Contains: x => x.'IntList.Contains(1)' == true
        /// IEnumerable.Contains x => x.'IntArray.Contains(2)' == true
        /// </summary>
        /// <param name="call"></param>
        private void VisitCallContains(MethodCallExpression call, FrpQueryType ct)
        {
            _findBool = true;

            //x => x.Foo.Bar.Contains('a')
            //x => x.IntList.Contains(1)
            if (call.Object is not null && call.Arguments.Count == 1)
            {
                CallArgsToQuery(call.Object, call.Arguments[0], ct);
                return;
            }

            //x => x.IntArray.Contains(2)
            if (call.Arguments.Count == 2)
            {
                CallArgsToQuery(call.Arguments[0], call.Arguments[1], ct);
                return;
            }

            throw new NotSupportedException($"Not supported method call {call.Method.Name}");
        }

        /// <summary>
        /// x => x.Foo.'StartsWith("foo")' == true
        /// x => x.Foo.'EndsWith("bar")' == true
        /// </summary>
        /// <param name="call"></param>
        private void VisitCallStartsWithOrEndsWith(MethodCallExpression call, FrpQueryType ct)
        {
            _findBool = true;

            if (call.Object is not null && call.Arguments.Count == 1)
            {
                CallArgsToQuery(call.Object, call.Arguments[0], ct);
                return;
            }

            throw new NotSupportedException($"Not supported method call {call.Method.Name}");
        }

        /// <summary>
        /// String.IndexOf: x => x.'Foo.Bar.IndexOf('a')' == 0
        /// List.IndexOf: x => x.'IntList.IndexOf(1)' == 10
        /// </summary>
        /// <param name="call"></param>
        private void VisitCallIndexOf(MethodCallExpression call, FrpQueryType ct)
        {
            if (call.Object is not null && call.Arguments.Count == 1)
            {
                CallArgsToQuery(call.Object, call.Arguments[0], ct);
                return;
            }

            throw new NotSupportedException($"Not supported method call {call.Method.Name}");
        }

        /// <summary>
        /// x => x.'Foo.Bar[0]' == 'a'
        /// x => x.'IntList[0]' == 10
        /// x => x.'IntList.ElementAt(0)' == 10
        /// </summary>
        /// <param name="call"></param>
        private void VisitCallArrayIndex(MethodCallExpression call, FrpQueryType ct)
        {
            //x => x.'Foo.Bar[0]' == 'a'
            //x => x.'IntList[0]' == 10
            if (call.Object is not null && call.Arguments.Count == 1)
            {
                CallArgsToQuery(call.Object, call.Arguments[0], ct);
                return;
            }

            //x => x.'IntList.ElementAt(0)' == 10
            if (call.Arguments.Count == 2)
            {
                CallArgsToQuery(call.Arguments[0], call.Arguments[1], ct);
                return;
            }

            throw new NotSupportedException($"Not supported method call {call.Method.Name}");
        }

        /// <summary>
        /// x => 'string.IsNullOrEmpty'(x.Foo) == true
        /// </summary>
        /// <param name="call"></param>
        private void VisitCallIsNullOrEmpty(MethodCallExpression call, FrpQueryType ct)
        {
            _findBool = true;

            if (call.Arguments.Count == 1)
            {
                Visit(call.Arguments[0]);
                _queries.Last().CallType = ct;
                return;
            }

            throw new NotSupportedException($"Not supported method call {call.Method.Name}");
        }

        /// <summary>
        /// x => x.Foo.'Equals("ab")' == true
        /// </summary>
        /// <param name="call"></param>
        private void VisitCallEquals(MethodCallExpression call, FrpQueryType ct)
        {
            _findBool = true;

            if (call.Object is not null && call.Arguments.Count == 1)
            {
                CallArgsToQuery(call.Object, call.Arguments[0], ct);
                return;
            }

            throw new NotSupportedException($"Not supported method call {call.Method.Name}");
        }

        /// <summary>
        /// IEnumerable.Contains x => x.'IntArray.Count(2)' == true
        /// </summary>
        /// <param name="call"></param>
        private void VisitCallCount(MethodCallExpression call, FrpQueryType ct)
        {
            _findBool = true;

            if (call.Arguments.Count == 1)
            {
                Visit(call.Arguments[0]);
                _queries.Last().CallType = ct;
                return;
            }

            throw new NotSupportedException($"Not supported method call {call.Method.Name}");
        }

        private void CallArgsToQuery(Expression ex1, Expression ex2, FrpQueryType ct)
        {
            FrpQuery q1 = new();
            GetQueryFromParameterOrConstantExpression(ex1, q1);

            FrpQuery q2 = new();
            GetQueryFromParameterOrConstantExpression(ex2, q2);

            q1.Value = q2.Value;
            q1.ValueType = q2.ValueType;
            q1.CallType = ct;
            _queries.Add(q1);
        }

        #endregion

        #region Helper

        private void GetQueryFromParameterOrConstantExpression(Expression ex, FrpQuery query)
        {
            object? constantValue = null;

            void ReadPath(Expression ex, FrpQuery query)
            {
                if (ex is ParameterExpression para && para.Name != null)
                {
                    query.IsMember = true;
                    query.Name = "$";
                    return;
                }
                else if (ex is MemberExpression mem && mem.Expression != null)
                {
                    ReadPath(mem.Expression, query);
                    if (query.IsMember)
                    {
                        query.Name += $".{Json.ToCamelCase(mem.Member.Name)}";
                    }
                    else if (constantValue is not null)
                    {
                        if (mem.Member is FieldInfo fi)
                        {
                            constantValue = fi.GetValue(constantValue);
                        }
                        else if (mem.Member is PropertyInfo pi)
                        {
                            constantValue = pi.GetValue(constantValue);
                        }
                    }

                    return;
                }
                else if (ex is ConstantExpression con)
                {
                    constantValue = con.Value;
                    return;
                }

                throw new NotSupportedException($"Not supported member expression {ex.NodeType}");
            }

            ReadPath(ex, query);

            if (query.IsMember)
            {
                query.MemberType = GetQueryType(ex.Type);
                if (query.MemberType == FrpQueryType.ValueBoolean)
                    _findBool = true;
            }
            else if (constantValue is not null)
            {
                query.Value = GetValueAsString(constantValue);
                query.ValueType = GetQueryType(constantValue.GetType());
            }
            else
            {
                query.Value = GetValueAsString(constantValue);
                query.ValueType = FrpQueryType.ValueNull;
            }
        }

        private static string GetValueAsString(object? value)
        {
            if (value is null)
                return "null";

            if (value is string s)
                return s;
            else if (value is bool b)
            {
                return b.ToString();
            }
            else
            {
                var type = value.GetType();
                if (IsNumber(type))
                {
                    if (value is double d)
                        return d.ToString(_ci);
                    else if (value is float f)
                        return f.ToString(_ci);
                    else if (value is decimal m)
                        return m.ToString(_ci);
                    else if (value.ToString() is string ns)
                        return ns;
                }
                else if (IsEnumerable(value.GetType()) || IsCollection(value.GetType()))
                {
                    return Json.GetJson(value);
                }
                else
                {
                    return Json.GetJson(value);
                }
            }

            return string.Empty;
        }

        private static FrpQueryType GetQueryType(Type type)
        {
            var qt = FrpQueryType.ValueObject;

            if (type == typeof(string) || type == typeof(char))
                qt = FrpQueryType.ValueString;
            else if (IsNumber(type))
                qt = FrpQueryType.ValueNumber;
            else if (IsNullable(type))
                qt = FrpQueryType.ValueNull;
            else if (type == typeof(bool))
                qt = FrpQueryType.ValueBoolean;
            else if (IsEnumerable(type) || IsCollection(type))
                qt = FrpQueryType.ValueArray;

            return qt;
        }

        private static FrpQueryType GetOperator(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Add: return FrpQueryType.QueryAdd;
                case ExpressionType.Multiply: return FrpQueryType.QueryMultiply;
                case ExpressionType.Subtract: return FrpQueryType.QuerySubtract;
                case ExpressionType.Divide: return FrpQueryType.QueryDivide;
                case ExpressionType.Equal: return FrpQueryType.QueryEqual;
                case ExpressionType.NotEqual: return FrpQueryType.QueryNotEqual;
                case ExpressionType.GreaterThan: return FrpQueryType.QueryGreaterThan;
                case ExpressionType.GreaterThanOrEqual: return FrpQueryType.QueryGreaterThanOrEqual;
                case ExpressionType.LessThan: return FrpQueryType.QueryLessThan;
                case ExpressionType.LessThanOrEqual: return FrpQueryType.QueryLessThanOrEqual;
                case ExpressionType.And: return FrpQueryType.QueryAnd;
                case ExpressionType.AndAlso: return FrpQueryType.QueryAndAlso;
                case ExpressionType.Or: return FrpQueryType.QueryOr;
                case ExpressionType.OrElse: return FrpQueryType.QueryOrElse;
                case ExpressionType.ArrayIndex: return FrpQueryType.CallArrayIndex;
                default:
                    break;
            }

            throw new NotSupportedException($"Operator not supported {nodeType}");
        }

        private static FrpQueryType GetCallType(string met)
        {
            return met switch
            {
                "Contains" => FrpQueryType.CallContains,
                "StartsWith" => FrpQueryType.CallStartWith,
                "EndsWith" => FrpQueryType.CallEndsWith,
                "Equals" => FrpQueryType.CallEquals,
                "ToLower" => FrpQueryType.CallToLower,
                "ToUpper" => FrpQueryType.CallToUpper,
                "IsNullOrEmpty" => FrpQueryType.CallIsNullOrEmpty,
                "Count" => FrpQueryType.CallCount,
                "get_Item" => FrpQueryType.CallArrayIndex,
                "ElementAt" => FrpQueryType.CallArrayIndex,
                "IndexOf" => FrpQueryType.CallIndexOf,
                _ => throw new NotSupportedException($"Call methode not supported {met}"),
            };
        }

        private static bool IsNumber(Type type)
        {
            return
                type == typeof(byte) ||
                type == typeof(short) ||
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(sbyte) ||
                type == typeof(ushort) ||
                type == typeof(uint) ||
                type == typeof(ulong) ||
                type == typeof(BigInteger) ||
                type == typeof(decimal) ||
                type == typeof(double) ||
                type == typeof(float);
        }

        private static bool IsNullable(Type type)
        {
            if (!type.GetTypeInfo().IsGenericType) return false;
            var g = type.GetGenericTypeDefinition();
            return g.Equals(typeof(Nullable<>));
        }

        private static bool IsEnumerable(Type type)
        {
            if (type == typeof(IEnumerable) || type.IsArray) return true;
            if (type == typeof(string)) return false; // do not define "String" as IEnumerable<char>

            foreach (var @interface in type.GetInterfaces())
            {
                if (@interface.GetTypeInfo().IsGenericType)
                {
                    if (@interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        // if needed, you can also return the type used as generic argument
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsCollection(Type type)
        {
            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(ICollection<>)))
                return true;
            else if (
                type.GetInterfaces().Any(x => x == typeof(ICollection) ||
                (x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>))))
                return true;

            return false;
        }

        #endregion
    }
}
