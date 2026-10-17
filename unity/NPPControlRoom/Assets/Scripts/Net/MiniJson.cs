using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NPP.Net
{
    /// <summary>
    /// Минимальный JSON-парсер/сериализатор (совместим с форматом MiniJSON, public domain).
    /// Unity JsonUtility не умеет произвольные словари — поэтому свой.
    /// </summary>
    public static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            return Parser.ParseValue(new Parser(json));
        }

        public static string Serialize(object obj)
        {
            var sb = new StringBuilder(256);
            Serializer.SerializeValue(obj, sb);
            return sb.ToString();
        }

        // ---------------- Serializer ----------------
        static class Serializer
        {
            public static void SerializeValue(object value, StringBuilder sb)
            {
                if (value == null) { sb.Append("null"); return; }
                var s = value as string;
                if (s != null) { SerializeString(s, sb); return; }

                if (value is bool) { sb.Append(((bool)value) ? "true" : "false"); return; }

                var dict = value as IDictionary;
                if (dict != null) { SerializeObject(dict, sb); return; }

                var list = value as IEnumerable;
                if (list != null) { SerializeArray(list, sb); return; }

                if (value is char) { SerializeString(value.ToString(), sb); return; }
                if (value is float || value is double || value is int || value is long ||
                    value is uint || value is ulong || value is short || value is byte ||
                    value is sbyte || value is ushort)
                {
                    sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    return;
                }
                SerializeString(value.ToString(), sb);
            }

            static void SerializeObject(IDictionary obj, StringBuilder sb)
            {
                sb.Append('{');
                bool first = true;
                foreach (object key in obj.Keys)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    SerializeString(key.ToString(), sb);
                    sb.Append(':');
                    SerializeValue(obj[key], sb);
                }
                sb.Append('}');
            }

            static void SerializeArray(IEnumerable arr, StringBuilder sb)
            {
                sb.Append('[');
                bool first = true;
                foreach (object item in arr)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    SerializeValue(item, sb);
                }
                sb.Append(']');
            }

            static void SerializeString(string str, StringBuilder sb)
            {
                sb.Append('"');
                foreach (char c in str)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            int code = c;
                            if (code < 32) sb.Append("\\u").Append(code.ToString("x4"));
                            else sb.Append(c);
                            break;
                    }
                }
                sb.Append('"');
            }
        }

        // ---------------- Parser ----------------
        class Parser
        {
            readonly string _json;
            int _index;

            public Parser(string json) { _json = json; _index = 0; }

            public static object ParseValue(Parser p)
            {
                p.SkipWhitespace();
                if (p._index >= p._json.Length) return null;
                char c = p._json[p._index];
                switch (c)
                {
                    case '{': return p.ParseObject();
                    case '[': return p.ParseArray();
                    case '"': return p.ParseString();
                    case 't':
                        p.Consume("true"); return true;
                    case 'f':
                        p.Consume("false"); return false;
                    case 'n':
                        p.Consume("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return p.ParseNumber();
                        throw new FormatException("Unexpected character '" + c + "' at " + p._index);
                }
            }

            void Consume(string literal)
            {
                if (_index + literal.Length > _json.Length ||
                    string.CompareOrdinal(_json, _index, literal, 0, literal.Length) != 0)
                    throw new FormatException("Expected '" + literal + "' at " + _index);
                _index += literal.Length;
            }

            Dictionary<string, object> ParseObject()
            {
                var dict = new Dictionary<string, object>();
                _index++; // {
                SkipWhitespace();
                if (Peek() == '}') { _index++; return dict; }
                while (true)
                {
                    SkipWhitespace();
                    if (Peek() != '"') throw new FormatException("Expected key at " + _index);
                    string key = ParseString();
                    SkipWhitespace();
                    if (Peek() != ':') throw new FormatException("Expected ':' at " + _index);
                    _index++;
                    object val = ParseValue(this);
                    dict[key] = val;
                    SkipWhitespace();
                    char c = Peek();
                    if (c == ',') { _index++; continue; }
                    if (c == '}') { _index++; break; }
                    throw new FormatException("Expected ',' or '}' at " + _index);
                }
                return dict;
            }

            List<object> ParseArray()
            {
                var list = new List<object>();
                _index++; // [
                SkipWhitespace();
                if (Peek() == ']') { _index++; return list; }
                while (true)
                {
                    object val = ParseValue(this);
                    list.Add(val);
                    SkipWhitespace();
                    char c = Peek();
                    if (c == ',') { _index++; continue; }
                    if (c == ']') { _index++; break; }
                    throw new FormatException("Expected ',' or ']' at " + _index);
                }
                return list;
            }

            string ParseString()
            {
                var sb = new StringBuilder();
                _index++; // opening quote
                while (_index < _json.Length)
                {
                    char c = _json[_index++];
                    if (c == '"') return sb.ToString();
                    if (c == '\\')
                    {
                        char e = _json[_index++];
                        switch (e)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                string hex = _json.Substring(_index, 4);
                                sb.Append((char)Convert.ToInt32(hex, 16));
                                _index += 4;
                                break;
                            default: throw new FormatException("Bad escape at " + _index);
                        }
                    }
                    else sb.Append(c);
                }
                throw new FormatException("Unterminated string");
            }

            object ParseNumber()
            {
                int start = _index;
                if (Peek() == '-') _index++;
                while (_index < _json.Length && (char.IsDigit(_json[_index]) || _json[_index] == '.' ||
                                                _json[_index] == 'e' || _json[_index] == 'E' ||
                                                _json[_index] == '+' || _json[_index] == '-'))
                    _index++;
                string num = _json.Substring(start, _index - start);
                double d;
                if (double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
                throw new FormatException("Bad number '" + num + "'");
            }

            char Peek() => _index < _json.Length ? _json[_index] : '\0';

            void SkipWhitespace()
            {
                while (_index < _json.Length && char.IsWhiteSpace(_json[_index])) _index++;
            }
        }
    }

    public static class JsonHelp
    {
        public static string Str(Dictionary<string, object> d, string key, string def = "")
        {
            object v;
            if (d != null && d.TryGetValue(key, out v) && v != null) return v.ToString();
            return def;
        }

        public static double Num(Dictionary<string, object> d, string key, double def = 0)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v) && v != null)
            {
                if (v is double) return (double)v;
                double parsed;
                if (double.TryParse(v.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)) return parsed;
            }
            return def;
        }

        public static int Int(Dictionary<string, object> d, string key, int def = 0)
        {
            return (int)Num(d, key, def);
        }

        public static bool Bool(Dictionary<string, object> d, string key, bool def = false)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v) && v != null)
            {
                if (v is bool) return (bool)v;
                if (v is double) return (double)v != 0;
            }
            return def;
        }

        public static List<object> Arr(Dictionary<string, object> d, string key)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v)) return v as List<object>;
            return null;
        }
    }
}
