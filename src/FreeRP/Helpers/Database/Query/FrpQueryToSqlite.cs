using FreeRP.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Helpers.Database
{
    public struct FrpQueryToSqlite
    {
        //https://www.sqlite.org/lang_corefunc.html
        //https://www.sqlite.org/json1.html

        private const string ArrayName = "arr";
        private readonly List<string> _arguments = [];
        private readonly List<FrpQuery> _queries = [];
        private readonly List<string> _selectFrom = [];
        private readonly StringBuilder _sb = new();
        private string _format = string.Empty;

        private FrpQuery _query = new();
        private int _queriesIndex = -1;
        private int _argumentIndex = -1;

        public static ValueTask<FormattableString> GetSql(IEnumerable<FrpQuery> queries)
        {
            FrpQueryToSqlite q = new(queries);
            return ValueTask.FromResult(q.CreateSql());
        }

        public FrpQueryToSqlite(IEnumerable<FrpQuery> queries)
        {
            _queries.AddRange(queries);
        }

        private string? _empty = null;
        private string GetEmpty()
        {
            if (_empty is null)
                _empty = AddArgument("");

            return _empty;
        }

        public FormattableString CreateSql()
        {
            while (TryGetNext())
            {
                if (_query.CallType == FrpQueryType.QueryNone)
                {
                    _sb.Append(MemberOrValueToSql());
                }
                else
                {
                    switch (_query.CallType)
                    {
                        case FrpQueryType.CallContains:
                            CallContains();
                            break;
                        case FrpQueryType.CallStartWith:
                        case FrpQueryType.CallEndsWith:
                            StartEndsWith();
                            break;
                        case FrpQueryType.CallEquals:
                            CallEquals();
                            break;
                        case FrpQueryType.CallToLower:
                            _sb.Append($"(lower({MemberOrValueToSql()}))");
                            break;
                        case FrpQueryType.CallToUpper:
                            _sb.Append($"(upper({MemberOrValueToSql()}))");
                            break;
                        case FrpQueryType.CallIsNullOrEmpty:
                            IsNullOrEmpty();
                            break;
                        case FrpQueryType.CallCount:
                            CallCount();
                            break;
                        case FrpQueryType.CallArrayIndex:
                            CallArrayIndex();
                            break;
                        case FrpQueryType.CallIndexOf:
                            CallIndexOf();
                            break;
                        default:
                            break;
                    }
                }

                if (_query.Next != FrpQueryType.QueryNone)
                    _sb.Append($" {GetSqlName(_query.Next)} ");
            }

            StringBuilder sbQuery = new();
            sbQuery.Append($"select * from {FrpQueryToFactory.TableName}");
            if (_selectFrom.Count > 0)
            {
                sbQuery.Append(", ");
                sbQuery.AppendJoin(", ", _selectFrom);
            }

            if (_sb.Length > 0)
            {
                sbQuery.Append(" where ");
                sbQuery.Append(_sb);
            }
            
            _format = sbQuery.ToString();

            return FormattableStringFactory.Create(_format, [.. _arguments]);
        }

        private bool TryGetNext()
        {
            _queriesIndex++;

            if (_queriesIndex < _queries.Count)
            {
                _query = _queries[_queriesIndex];
                return true;
            }

            return false;
        }

        string AddArgument(string arg)
        {
            _argumentIndex++;
            _arguments.Add(arg);
            return $"{{{_argumentIndex}}}";
        }

        private void CallContains()
        {
            if (_query.IsMember && _query.MemberType == FrpQueryType.ValueString)
                StartEndsWith();
            else if (_query.IsMember && _query.MemberType == FrpQueryType.ValueArray)
            {
                var arr = $"{ArrayName}{_queriesIndex}";
                _selectFrom.Add($"json_each({FrpQueryToFactory.TableName}.{FrpQueryToFactory.JsonColName}, {AddArgument(_query.Name)}) {arr}");

                if (_query.Next == FrpQueryType.QueryEqual || _query.Next == FrpQueryType.QueryOrElse)
                {
                    string q = $"{arr}.value = {GetValue(_query.ValueType, _query.Value)}";
                    if (TryGetNext() && _query.Value == (false).ToString())
                        q = q.Replace("=", "!=");

                    _sb.Append(q);
                }
                else
                    _sb.Append($"{arr}.value = {GetValue(_query.ValueType, _query.Value)}");
            }
        }

        private void StartEndsWith()
        {
            _sb.Append(MemberOrValueToSql());
            string val;
            if (_query.CallType == FrpQueryType.CallStartWith)
                val = $"{AddArgument($"{_query.Value}%")}";
            else if (_query.CallType == FrpQueryType.CallEndsWith)
                val = $"{AddArgument($"%{_query.Value}")}";
            else
                val = $"{AddArgument($"%{_query.Value}%")}";

            if ((_query.Next == FrpQueryType.QueryEqual || _query.Next == FrpQueryType.QueryNotEqual))
            {
                if (TryGetNext() && _query.ValueType == FrpQueryType.ValueBoolean)
                {
                    if (_query.Value == (true).ToString())
                        _sb.Append($" like {val}");
                    else
                        _sb.Append($" not like {val}");
                }
            }
            else
            {
                _sb.Append($" like {val}");
            }
        }

        private void CallEquals()
        {
            _sb.Append(MemberOrValueToSql());

            if (_query.Next == FrpQueryType.QueryEqual || _query.Next == FrpQueryType.QueryNotEqual)
            {
                var vt = _query.ValueType;
                var val = _query.Value;

                _sb.Append($" {GetSqlName(_query.Next)} ");
                if (TryGetNext() && _query.ValueType == FrpQueryType.ValueBoolean)
                {
                    _sb.Append(GetValue(vt, val));
                }
            }
            else
            {
                _sb.Append($" = {GetValue(_query.ValueType, _query.Value)}");
            }
        }

        private void IsNullOrEmpty()
        {
            var mem = MemberOrValueToSql();
            _sb.Append(mem);

            if (_query.Next == FrpQueryType.QueryEqual || _query.Next == FrpQueryType.QueryNotEqual)
            {
                if (TryGetNext() && _query.ValueType == FrpQueryType.ValueBoolean)
                {
                    if (_query.Value == (true).ToString())
                        _sb.Append($" is null or {mem} = {GetEmpty()}");
                    else
                        _sb.Append($" is not null and {mem} != {GetEmpty()}");
                }
            }
            else
            {
                _sb.Append($" is null or {mem} = {GetEmpty()}");
            }
        }

        private void CallCount()
        {
            if (_query.IsMember && _query.MemberType == FrpQueryType.ValueString)
            {
                _sb.Append($"length({MemberOrValueToSql()})");
            }
            else if (_query.IsMember && _query.MemberType == FrpQueryType.ValueArray)
            {
                _sb.Append($"json_array_length({MemberOrValueToSql()})");
            }
        }

        private void CallArrayIndex()
        {
            _sb.Append($"json_extract({FrpQueryToFactory.TableName}.{FrpQueryToFactory.JsonColName}, {AddArgument($"{_query.Name}[{_query.Value}]")})");
        }

        private void CallIndexOf()
        {
            if (_query.MemberType == FrpQueryType.ValueString)
            {
                _sb.Append($"instr({MemberOrValueToSql()}, {GetValue(_query.ValueType, _query.Value)}) {GetSqlName(_query.Next)}");
                if (TryGetNext() && int.TryParse(_query.Value, out int c))
                {
                    c++;
                    _sb.Append($" {c}");
                }
            }
            else if (_query.MemberType == FrpQueryType.ValueArray)
            {
                var arr = $"{ArrayName}{_queriesIndex}";
                _selectFrom.Add($"json_each({FrpQueryToFactory.TableName}.{FrpQueryToFactory.JsonColName}, {AddArgument(_query.Name)}) {arr}");
                _sb.Append($"{arr}.key = {GetValue(_query.ValueType, _query.Value)}");
                if (TryGetNext())
                {
                    _sb.Append($" and {arr}.value = {GetValue(_query.ValueType, _query.Value)}");
                }
            }
        }

        private string MemberOrValueToSql()
        {
            if (_query.IsMember)
            {
                return $"json_extract({FrpQueryToFactory.TableName}.{FrpQueryToFactory.JsonColName}, {AddArgument(_query.Name)})";
            }

            return GetValue(_query.ValueType, _query.Value);
        }

        private static string GetSqlName(FrpQueryType queryType)
        {
            return queryType switch
            {
                FrpQueryType.QueryAdd => "+",
                FrpQueryType.QueryDivide => "/",
                FrpQueryType.QueryMultiply => "*",
                FrpQueryType.QuerySubtract => "-",
                FrpQueryType.QueryGreaterThan => ">",
                FrpQueryType.QueryGreaterThanOrEqual => ">=",
                FrpQueryType.QueryLessThan => "<",
                FrpQueryType.QueryLessThanOrEqual => "<=",
                FrpQueryType.QueryEqual => "=",
                FrpQueryType.QueryNotEqual => "!=",
                FrpQueryType.QueryAnd => "&",
                FrpQueryType.QueryOr => "|",
                FrpQueryType.QueryAndAlso => "and",
                FrpQueryType.QueryOrElse => "or",
                _ => ""
            };
        }

        private string GetValue(FrpQueryType queryType, string val)
        {
            return queryType switch
            {
                FrpQueryType.ValueString => $"{AddArgument(val)}",
                FrpQueryType.ValueArray or
                FrpQueryType.ValueNumber or
                FrpQueryType.ValueBoolean or
                FrpQueryType.ValueObject => val,
                _ => "'null'"
            };
        }
    }
}
