using UnityEngine;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WingroveAudio
{
    public class AudioNameGroup : ScriptableObject
    {                
        [SerializeField]
        private string[] m_events;
        
        [SerializeField]
        private string[] m_parameters;

        public string[] GetEvents()
        {
            if(m_events == null)
            {
                m_events = new string[] { };
            }
            return m_events;
        }
        
        public string[] GetParameters()
        {
            if (m_parameters == null)
            {
                m_parameters = new string[] { };
            }
            return m_parameters;
        }

        public void AddEvent(string eventName)
        {
#if UNITY_EDITOR
            ArrayUtility.Add(ref m_events, eventName);
#endif
        }

        public void AddParameter(string eventName)
        {
#if UNITY_EDITOR
            ArrayUtility.Add(ref m_parameters, eventName);
#endif
        }

        private string SanitiseString(string inputString)
        {
            string[] splits = inputString.Split(" ,_.;".ToCharArray());

            string result = "";
            int wordIndex = 0;
            foreach (string s in splits)
            {
                int index = 0;
                foreach(char c in s)
                {
                    if (index == 0)
                    {
                        if(char.IsLetterOrDigit(c))
                        {
                            if(wordIndex==0)
                            {
                                if(char.IsLetter(c))
                                {
                                    result += char.ToUpperInvariant(c);
                                }
                                else
                                {
                                    result += "K" + c;
                                }
                            }
                            else
                            {
                                result += char.ToUpperInvariant(c);
                            }
                        }
                        else
                        {
                            result += "K";
                        }
                    }
                    else
                    {
                        if (char.IsLetterOrDigit(c))
                        {
                            result += char.ToLowerInvariant(c);
                        }
                    }
                    index++;
                }
                wordIndex++;
            }
            return result;
        }

        public string GenerateStaticCSharp()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("namespace AudioNames");
            sb.AppendLine("{");

            sb.AppendLine("    public static class " + name);
            sb.AppendLine("    {");

            sb.AppendLine("        public static class Events");
            sb.AppendLine("        {");

            foreach (string e in m_events)
            {
                sb.AppendLine("            public const string " + SanitiseString(e) + " = \"" + e + "\";");
            }

            sb.AppendLine("        }");


            sb.AppendLine("        public static class Parameters");
            sb.AppendLine("        {");

            foreach (string e in m_parameters)
            {
                sb.AppendLine("            public const string " + SanitiseString(e) + " = \"" + e + "\";");
            }

            sb.AppendLine("        }");

            sb.AppendLine("    }");

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}