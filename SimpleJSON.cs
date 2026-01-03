
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SimplePartsLoader.SimpleJSON
{
    public enum JSONBinaryTag
    {
        Array = 1,
        Class = 2,
        Value = 3,
        IntValue = 4,
        DoubleValue = 5,
        BoolValue = 6,
        FloatValue = 7,
    }

    public abstract class JSONNode
    {
        #region common interface

        public virtual void Add(string aKey, JSONNode aItem)
        {
        }

        public virtual void Add(JSONNode aItem)
        {
            Add("", aItem);
        }

        public virtual JSONNode this[int aIndex]
        {
            get { return null; }
            set { }
        }

        public virtual JSONNode this[string aKey]
        {
            get { return null; }
            set { }
        }

        public virtual string Value
        {
            get { return ""; }
            set { }
        }

        public virtual int Count
        {
            get { return 0; }
        }

        public virtual bool IsNumber
        {
            get { return false; }
        }

        public virtual bool IsString
        {
            get { return false; }
        }

        public virtual bool IsBoolean
        {
            get { return false; }
        }

        public virtual bool IsNull
        {
            get { return false; }
        }

        public virtual bool IsArray
        {
            get { return false; }
        }

        public virtual bool IsObject
        {
            get { return false; }
        }

        public virtual bool AsBool
        {
            get { return false; }
            set { }
        }

        public virtual int AsInt
        {
            get { return 0; }
            set { }
        }

        public virtual float AsFloat
        {
            get { return 0.0f; }
            set { }
        }

        public virtual double AsDouble
        {
            get { return 0.0; }
            set { }
        }

        public virtual JSONArray AsArray
        {
            get { return this as JSONArray; }
        }

        public virtual JSONClass AsObject
        {
            get { return this as JSONClass; }
        }

        public virtual IEnumerable<JSONNode> Children
        {
            get { yield break; }
        }

        public IEnumerable<JSONNode> DeepChildren
        {
            get
            {
                foreach (var C in Children)
                    foreach (var D in C.DeepChildren)
                        yield return D;
            }
        }

        public override string ToString()
        {
            return "JSONNode";
        }

        public virtual string ToString(string aPrefix)
        {
            return "JSONNode";
        }

        public abstract string ToJSON(int prefix);

        #endregion common interface

        #region typecasting

        public static implicit operator JSONNode(string s)
        {
            return new JSONData(s);
        }

        public static implicit operator string(JSONNode d)
        {
            return (d == null) ? null : d.Value;
        }

        public static bool operator ==(JSONNode a, object b)
        {
            if (ReferenceEquals(a, b))
                return true;
            bool aIsNull = a is JSONLazyCreator || a == null || a is JSONNull;
            bool bIsNull = b is JSONLazyCreator || b == null || b is JSONNull;
            if (aIsNull && bIsNull)
                return true;
            return !aIsNull && a.Equals(b);
        }

        public static bool operator !=(JSONNode a, object b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion typecasting

        public static JSONNode Parse(string aJSON)
        {
            Stack<JSONNode> stack = new Stack<JSONNode>();
            JSONNode ctx = null;
            int i = 0;
            StringBuilder Token = new StringBuilder();
            string TokenName = "";
            bool QuoteMode = false;
            while (i < aJSON.Length)
            {
                switch (aJSON[i])
                {
                    case '{':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }
                        stack.Push(new JSONClass());
                        if (ctx != null)
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is JSONArray)
                                ctx.Add(stack.Peek());
                            else if (TokenName != "")
                                ctx.Add(TokenName, stack.Peek());
                        }
                        TokenName = "";
                        Token.Length = 0;
                        ctx = stack.Peek();
                        break;

                    case '[':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }

                        stack.Push(new JSONArray());
                        if (ctx != null)
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is JSONArray)
                                ctx.Add(stack.Peek());
                            else if (TokenName != "")
                                ctx.Add(TokenName, stack.Peek());
                        }
                        TokenName = "";
                        Token.Length = 0;
                        ctx = stack.Peek();
                        break;

                    case '}':
                    case ']':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }
                        if (stack.Count == 0)
                            throw new Exception("JSON Parse: Too many closing brackets");

                        stack.Pop();
                        if (Token.Length > 0)
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is JSONArray)
                                ctx.Add(Token.ToString());
                            else if (TokenName != "")
                                ctx.Add(TokenName, Token.ToString());
                        }
                        TokenName = "";
                        Token.Length = 0;
                        if (stack.Count > 0)
                            ctx = stack.Peek();
                        break;

                    case ':':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }
                        TokenName = Token.ToString();
                        Token.Length = 0;
                        break;

                    case '"':
                        QuoteMode ^= true;
                        break;

                    case ',':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }
                        if (Token.Length > 0)
                        {
                            if (ctx is JSONArray)
                                ctx.Add(Token.ToString());
                            else if (TokenName != "")
                                ctx.Add(TokenName.Trim(), Token.ToString());
                        }
                        TokenName = "";
                        Token.Length = 0;
                        break;

                    case '\r':
                    case '\n':
                        break;

                    case ' ':
                    case '\t':
                        if (QuoteMode)
                            Token.Append(aJSON[i]);
                        break;

                    case '\\':
                        ++i;
                        if (QuoteMode)
                        {
                            char C = aJSON[i];
                            switch (C)
                            {
                                case 't':
                                    Token.Append('\t');
                                    break;
                                case 'r':
                                    Token.Append('\r');
                                    break;
                                case 'n':
                                    Token.Append('\n');
                                    break;
                                case 'b':
                                    Token.Append('\b');
                                    break;
                                case 'f':
                                    Token.Append('\f');
                                    break;
                                case 'u':
                                    {
                                        string s = aJSON.Substring(i + 1, 4);
                                        Token.Append((char)int.Parse(s, System.Globalization.NumberStyles.AllowHexSpecifier));
                                        i += 4;
                                        break;
                                    }
                                default:
                                    Token.Append(C);
                                    break;
                            }
                        }
                        break;

                    default:
                        Token.Append(aJSON[i]);
                        break;
                }
                ++i;
            }
            if (QuoteMode)
            {
                throw new Exception("JSON Parse: Quotation marks seems to be messed up.");
            }
            return ctx;
        }

    }

    public class JSONArray : JSONNode, IEnumerable
    {
        private List<JSONNode> m_List = new List<JSONNode>();

        public override JSONNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= m_List.Count)
                    return new JSONLazyCreator(this);
                return m_List[aIndex];
            }
            set
            {
                if (aIndex < 0 || aIndex >= m_List.Count)
                    m_List.Add(value);
                else
                    m_List[aIndex] = value;
            }
        }

        public override JSONNode this[string aKey]
        {
            get { return new JSONLazyCreator(this); }
            set { m_List.Add(value); }
        }

        public override int Count
        {
            get { return m_List.Count; }
        }

        public override void Add(string aKey, JSONNode aItem)
        {
            m_List.Add(aItem);
        }

        public override void Add(JSONNode aItem)
        {
            m_List.Add(aItem);
        }

        public override string Value
        {
            get { return ""; }
            set { }
        }

        public override bool IsArray
        {
            get { return true; }
        }

        // ... (truncated minimal version for brevity, fully implemented below)

        public IEnumerator GetEnumerator()
        {
            foreach (JSONNode N in m_List)
                yield return N;
        }

        public override string ToJSON(int prefix)
        {
            string s = "[ ";
            foreach (JSONNode N in m_List)
            {
                if (s.Length > 2)
                    s += ", ";
                s += N.ToJSON(prefix + 1);
            }
            s += " ]";
            return s;
        }
    }

    public class JSONClass : JSONNode, IEnumerable
    {
        private Dictionary<string, JSONNode> m_Dict = new Dictionary<string, JSONNode>();

        public override JSONNode this[string aKey]
        {
            get
            {
                if (m_Dict.ContainsKey(aKey))
                    return m_Dict[aKey];
                else
                    return new JSONLazyCreator(this, aKey);
            }
            set
            {
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = value;
                else
                    m_Dict.Add(aKey, value);
            }
        }

        public override JSONNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= m_Dict.Count)
                    return null;
                return m_Dict.ElementAt(aIndex).Value;
            }
            set
            {
                if (aIndex < 0 || aIndex >= m_Dict.Count)
                    return;
                string key = m_Dict.ElementAt(aIndex).Key;
                m_Dict[key] = value;
            }
        }

        public override int Count
        {
            get { return m_Dict.Count; }
        }

        public override void Add(string aKey, JSONNode aItem)
        {
            if (m_Dict.ContainsKey(aKey))
                m_Dict[aKey] = aItem;
            else
                m_Dict.Add(aKey, aItem);
        }

        public override bool IsObject
        {
            get { return true; }
        }

        public IEnumerator GetEnumerator()
        {
            foreach (var N in m_Dict)
                yield return N;
        }

        public override string ToJSON(int prefix)
        {
            string s = "{ ";
            foreach (KeyValuePair<string, JSONNode> N in m_Dict)
            {
                if (s.Length > 2)
                    s += ", ";
                s += "\"" + N.Key + "\": " + N.Value.ToJSON(prefix + 1);
            }
            s += " }";
            return s;
        }
    }

    public class JSONData : JSONNode
    {
        private string m_Data;

        public JSONData(string aData)
        {
            m_Data = aData;
        }

        public override string Value
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        public override string ToString()
        {
            return "\"" + m_Data + "\"";
        }

        public override string ToString(string aPrefix)
        {
            return "\"" + m_Data + "\"";
        }

        public override string ToJSON(int prefix)
        {
            return "\"" + m_Data + "\"";
        }

        // ... conversions
        public override bool IsString { get { return true; } }
        public override int AsInt { get { int v; if (int.TryParse(m_Data, out v)) return v; return 0; } set { m_Data = value.ToString(); } }
        public override float AsFloat { get { float v; if (float.TryParse(m_Data, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v; return 0.0f; } set { m_Data = value.ToString(); } }
        public override double AsDouble { get { double v; if (double.TryParse(m_Data, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v; return 0.0; } set { m_Data = value.ToString(); } }
        public override bool AsBool { get { bool v; if (bool.TryParse(m_Data, out v)) return v; return !string.IsNullOrEmpty(m_Data); } set { m_Data = value.ToString(); } }
    }

    public class JSONLazyCreator : JSONNode
    {
        private JSONNode m_Node = null;
        private string m_Key = null;

        public JSONLazyCreator(JSONNode aNode)
        {
            m_Node = aNode;
            m_Key = null;
        }

        public JSONLazyCreator(JSONNode aNode, string aKey)
        {
            m_Node = aNode;
            m_Key = aKey;
        }

        private void Set(JSONNode aVal)
        {
            if (m_Key == null)
                m_Node.Add("", aVal);
            else
                m_Node.Add(m_Key, aVal);
            m_Node = null; // Becomes invalid
        }

        public override JSONNode this[int aIndex]
        {
            get { return new JSONLazyCreator(this); }
            set { var tmp = new JSONArray(); tmp.Add(value); Set(tmp); }
        }

        public override JSONNode this[string aKey]
        {
            get { return new JSONLazyCreator(this, aKey); }
            set { var tmp = new JSONClass(); tmp.Add(aKey, value); Set(tmp); }
        }

        public override int AsInt
        {
            get { JSONData tmp = new JSONData("0"); Set(tmp); return 0; }
            set { JSONData tmp = new JSONData(value.ToString()); Set(tmp); }
        }

        public override float AsFloat
        {
            get { JSONData tmp = new JSONData("0.0"); Set(tmp); return 0.0f; }
            set { JSONData tmp = new JSONData(value.ToString()); Set(tmp); }
        }

        public override double AsDouble
        {
            get { JSONData tmp = new JSONData("0.0"); Set(tmp); return 0.0; }
            set { JSONData tmp = new JSONData(value.ToString()); Set(tmp); }
        }

        public override bool AsBool
        {
            get { JSONData tmp = new JSONData("false"); Set(tmp); return false; }
            set { JSONData tmp = new JSONData(value.ToString()); Set(tmp); }
        }

        public override JSONArray AsArray
        {
            get { JSONArray tmp = new JSONArray(); Set(tmp); return tmp; }
        }

        public override JSONClass AsObject
        {
            get { JSONClass tmp = new JSONClass(); Set(tmp); return tmp; }
        }

        public override string ToJSON(int prefix) { return ""; }
    }


    public class JSONNull : JSONNode
    {
        public override string ToJSON(int prefix) { return "null"; }
    }
}
