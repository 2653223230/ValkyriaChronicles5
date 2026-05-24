using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TcgEngine
{
    /// <summary>
    /// RFC4180 CSV 读写（UTF-8 BOM），供注册表与策划表共用。
    /// </summary>
    public static class Vc5CsvIO
    {
        public static List<Dictionary<string, string>> ReadRows(string csvText)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
            if (string.IsNullOrEmpty(csvText))
                return result;

            List<string[]> rawRows = Parse(csvText);
            if (rawRows.Count < 1)
                return result;

            string[] headers = rawRows[0];
            for (int i = 1; i < rawRows.Count; i++)
            {
                string[] values = rawRows[i];
                Dictionary<string, string> row = new Dictionary<string, string>();
                for (int c = 0; c < headers.Length; c++)
                {
                    string key = headers[c];
                    if (string.IsNullOrEmpty(key))
                        continue;
                    string val = c < values.Length ? values[c] : "";
                    row[key] = val;
                }
                if (RowHasContent(row))
                    result.Add(row);
            }
            return result;
        }

        public static string WriteRows(string[] headers, List<Dictionary<string, string>> rows)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('\uFEFF');
            sb.AppendLine(string.Join(",", EscapeHeaders(headers)));
            foreach (Dictionary<string, string> row in rows)
            {
                List<string> cells = new List<string>();
                foreach (string h in headers)
                    cells.Add(EscapeField(Get(row, h, "")));
                sb.AppendLine(string.Join(",", cells));
            }
            return sb.ToString();
        }

        public static string ReadTextFile(string path)
        {
            if (!File.Exists(path))
                return null;
            byte[] b = File.ReadAllBytes(path);
            if (b.Length == 0)
                return string.Empty;

            int off = 0;
            if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF)
                off = 3;

            Encoding strictUtf8 = Encoding.GetEncoding("utf-8", new EncoderExceptionFallback(), new DecoderExceptionFallback());
            try
            {
                return strictUtf8.GetString(b, off, b.Length - off);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(936).GetString(b);
            }
        }

        public static void WriteTextFileUtf8Bom(string path, string content)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, content, new UTF8Encoding(true));
        }

        public static string Get(Dictionary<string, string> row, string key, string def = "")
        {
            if (row == null || !row.TryGetValue(key, out string v) || string.IsNullOrEmpty(v))
                return def;
            return v;
        }

        public static int ParseInt(string s, int def = 0)
        {
            if (string.IsNullOrWhiteSpace(s))
                return def;
            return int.TryParse(s.Trim(), out int v) ? v : def;
        }

        public static bool ParseBool01(string s, bool def = true)
        {
            if (string.IsNullOrWhiteSpace(s))
                return def;
            s = s.Trim();
            if (s == "0" || s.Equals("false", System.StringComparison.OrdinalIgnoreCase))
                return false;
            if (s == "1" || s.Equals("true", System.StringComparison.OrdinalIgnoreCase))
                return true;
            return def;
        }

        private static string[] EscapeHeaders(string[] headers)
        {
            string[] outH = new string[headers.Length];
            for (int i = 0; i < headers.Length; i++)
                outH[i] = EscapeField(headers[i]);
            return outH;
        }

        private static string EscapeField(string value)
        {
            if (value == null)
                value = "";
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static bool RowHasContent(Dictionary<string, string> row)
        {
            foreach (KeyValuePair<string, string> kv in row)
            {
                if (!string.IsNullOrWhiteSpace(kv.Value))
                    return true;
            }
            return false;
        }

        private static List<string[]> Parse(string text)
        {
            if (!string.IsNullOrEmpty(text) && text[0] == '\uFEFF')
                text = text.Substring(1);

            List<string[]> rows = new List<string[]>();
            List<string> row = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                        inQuotes = true;
                    else if (c == ',')
                    {
                        row.Add(sb.ToString());
                        sb.Length = 0;
                    }
                    else if (c == '\n')
                    {
                        row.Add(sb.ToString());
                        sb.Length = 0;
                        if (HasContent(row))
                            rows.Add(row.ToArray());
                        row.Clear();
                    }
                    else if (c != '\r')
                    {
                        sb.Append(c);
                    }
                }
            }

            row.Add(sb.ToString());
            if (HasContent(row))
                rows.Add(row.ToArray());
            return rows;
        }

        private static bool HasContent(List<string> row)
        {
            foreach (string s in row)
            {
                if (!string.IsNullOrWhiteSpace(s))
                    return true;
            }
            return false;
        }
    }
}
