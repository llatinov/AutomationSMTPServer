using System;
using System.Text;

namespace AutomationRhapsody.AutomationSMTPServer
{
    /// <summary>
    /// Helper functions for text handling
    /// </summary>
    public class TextFunctions
    {
        #region Head/Tail
        /// <summary>
        /// Get the head of a string, 
        /// 
        /// destructive, ie leaving only the body in the text variable
        /// </summary>
        /// <param name="text">Text string</param>
        /// <param name="uptoItem">Head cut of point (neck)</param>
        /// <param name="comparisonType">String Comparison</param>
        /// <returns>Head only</returns>
        public static string Head(ref string text, string uptoItem, StringComparison comparisonType)
        {
            int position = text.IndexOf(uptoItem, comparisonType);
            string headText;

            if (position == -1)
            {
                headText = text;
                text = string.Empty;
            }
            else
            {
                headText = text.Substring(0, position);
                text = text.Substring(position + uptoItem.Length);
            }

            return headText;
        }

        /// <summary>
        /// Get the head of a string, 
        /// 
        /// destructive, ie leaving only the body in the text variable
        /// case sensitive
        /// </summary>
        /// <param name="text">Text string</param>
        /// <param name="uptoItem">Head cut of point (neck)</param>
        /// <returns>Head only</returns>
        public static string Head(ref string text, string uptoItem)
        {
            return Head(ref text, uptoItem, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Get the head of a string
        /// 
        /// non destuctive, ie leaves the text as it was
        /// </summary>
        /// <param name="text">Text string</param>
        /// <param name="uptoItem">Head cut of point (neck)</param>
        /// <param name="comparisonType">String Comparison</param>
        /// <returns>Head only</returns>
        public static string Head(string text, string uptoItem, StringComparison comparisonType)
        {
            int position = text.IndexOf(uptoItem, comparisonType);
            string headText;

            if (position == -1)
            {
                headText = text;
            }
            else
            {
                headText = text.Substring(0, position);
            }

            return headText;
        }

        /// <summary>
        /// Get the head of a string
        /// 
        /// non destuctive, ie leaves the text as it was
        /// case sensitive
        /// </summary>
        /// <param name="text">Text string</param>
        /// <param name="uptoItem">Head cut of point (neck)</param>
        /// <returns>Head only</returns>
        public static string Head(string text, string uptoItem)
        {
            return Head(text, uptoItem, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Remove the tail from text
        /// 
        /// destructive, ie leaving only the body in the text variable
        /// </summary>
        /// <param name="text">Text string</param>
        /// <param name="uptoItem">Tail cut off point</param>
        /// <param name="comparisonType">String Comparison</param>
        /// <returns>Tail only</returns>
        public static string Tail(ref string text, string uptoItem, StringComparison comparisonType)
        {
            int position = text.LastIndexOf(uptoItem, comparisonType);
            string tailText;

            if (position == -1)
            {
                tailText = text;
                text = string.Empty;
            }
            else
            {
                tailText = text.Substring(position + uptoItem.Length);
                text = text.Substring(0, position);
            }

            return tailText;
        }

        /// <summary>
        /// Remove the tail from text
        /// 
        /// destructive, ie leaving only the body in the text variable
        /// case sensitive
        /// </summary>
        /// <param name="text">Text string</param>
        /// <param name="uptoItem">Tail cut off point</param>
        /// <returns>Tail only</returns>
        public static string Tail(ref string text, string uptoItem)
        {
            return Tail(ref text, uptoItem, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Remove the tail from text
        /// 
        /// non destuctive, ie leaves the text as it was
        /// </summary>
        /// <param name="text">Text string</param>
        /// <param name="uptoItem">Tail cut off point</param>
        /// <param name="comparisonType">String Comparison</param>
        /// <returns>Tail only</returns>
        public static string Tail(string text, string uptoItem, StringComparison comparisonType)
        {
            int position = text.LastIndexOf(uptoItem, comparisonType);
            string tailText;

            if (position == -1)
            {
                tailText = text;
            }
            else
            {
                tailText = text.Substring(position + uptoItem.Length);
            }

            return tailText;
        }

        /// <summary>
        /// Remove the tail from text
        /// 
        /// non destuctive, ie leaves the text as it was
        /// case sensitive
        /// </summary>
        /// <param name="text">Text string</param>
        /// <param name="uptoItem">Tail cut off point</param>
        /// <returns>Tail only</returns>
        public static string Tail(string text, string uptoItem)
        {
            return Tail(text, uptoItem, StringComparison.CurrentCulture);
        }
        #endregion

        #region Between
        /// <summary>
        /// <para>Get the text between the 'startsWith' and 'endsWith' parameters</para>
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="startsWith">Required text starts with this</param>
        /// <param name="endsWith">Required text ends with this</param>
        /// <param name="comparisonType">String Comparison</param>
        /// <returns>Text between</returns>
        public static string Between(string text, string startsWith, string endsWith, StringComparison comparisonType)
        {
            Head(ref text, startsWith, comparisonType);
            return Head(text, endsWith, comparisonType);
        }

        /// <summary>
        /// <para>Get the text between the 'startsWith' and 'endsWith' parameters</para>
        /// <para>Case sensitive</para>
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="startsWith">Required text starts with this</param>
        /// <param name="endsWith">Required text ends with this</param>
        /// <returns>Text between</returns>
        public static string Between(string text, string startsWith, string endsWith)
        {
            return Between(text, startsWith, endsWith, StringComparison.CurrentCulture);
        }
        #endregion

        #region Remove
        /// <summary>
        /// <para>Remove chars from a string</para>
        /// </summary>
        internal static string Remove(string text, char[] chars)
        {
            StringBuilder newText = new StringBuilder();
            for (int index = 0; index < text.Length; index++)
            {
                if (Array.IndexOf<char>(chars, text[index]) == -1)
                {
                    newText.Append(text[index]);
                }
            }
            return newText.ToString();
        }
        #endregion
    }
}