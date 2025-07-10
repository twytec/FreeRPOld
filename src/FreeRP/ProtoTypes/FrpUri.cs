using System.Diagnostics.CodeAnalysis;

namespace FreeRP
{
    public partial class FrpUri
    {
        public List<string> Segments { get; set; } = [];
        public string Host { get; set; } = string.Empty;

        public FrpUri(string uri)
        {
            if (uri.Length == 0)
                throw new ArgumentNullException(uri);

            var sp = uri.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length > 0)
            {
                Scheme = sp[0].ToLower() switch
                {
                    "file:" => FrpUriScheme.File,
                    "db:" => FrpUriScheme.Database,
                    "plugin:" => FrpUriScheme.Plugin,
                    _ => throw new NotSupportedException(),
                };

                foreach (var item in sp.Skip(1))
                {
                    if (Host == string.Empty)
                        Host = item;

                    Segments.Add(item);
                }
            }
            else
                throw new ArgumentNullException(uri);
        }

        public static bool TryCreate(string uri, [MaybeNullWhen(false)] out FrpUri frpUri)
        {
            try
            {
                frpUri = new FrpUri(uri);
                return true;
            }
            catch (Exception)
            {
                frpUri = null;
                return false;
            }
        }

        public string AddChild(string u)
        {
            if (u.Length > 0 && u.Trim('/') is string s && s.Length > 0)
                Segments.Add(s);

            return GetUriAsString();
        }

        public bool TryGetParent([MaybeNullWhen(false)] out FrpUri frpUri)
        {
            if (Segments.Count > 0)
            {
                var s = Segments.ToList();
                s.RemoveAt(s.Count - 1);
                if (s.Count > 0)
                    frpUri = new($"{SchemeToString()}{string.Join("/", s)}");
                else
                    frpUri = new($"{SchemeToString()}");

                return true;
            }

            frpUri = null;
            return false;
        }

        public string ToAbsolutPath(string frpRoot) =>
            Path.Combine(frpRoot, string.Join("\\", Segments));

        public string SchemeToString()
        {
            return Scheme switch
            {
                FrpUriScheme.File => $"file://",
                FrpUriScheme.Database => $"db://",
                _ => ""
            };
        }

        public string GetUriAsString()
        {
            if (Segments.Count > 0)
                return $"{SchemeToString()}{string.Join("/", Segments)}";
            else
                return $"{SchemeToString()}";
        }
    }
}
