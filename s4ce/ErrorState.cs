using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace s4ce
{
    public class ErrorState
    {
        public bool HasErrors { get; set; }
        public bool HasWarnings { get; set; }
        public Dictionary<Control, String> Errors { get; set; }
        public Dictionary<Control, String> Warnings { get; set; }
        public ErrorState()
        {
            Errors = new Dictionary<Control, string>();
            Warnings = new Dictionary<Control, string>();
        }

        internal void AddError(Control ctl, String fmt)
        {
            if (Errors.ContainsKey(ctl))
                Errors[ctl] = String.Format("{0}{1}{2}", Errors[ctl], Environment.NewLine, fmt);
            else
                Errors[ctl] = fmt;
            HasErrors = true;
        }
        internal void AddWarning(Control ctl, String fmt)
        {
            if (Warnings.ContainsKey(ctl))
                Warnings[ctl] = String.Format("{0}{1}{2}", Warnings[ctl], Environment.NewLine, fmt);
            else
                Warnings[ctl] = fmt;
            HasWarnings = true;
        }

        public string WarningSTR
        {
            get
            {
                if (!HasWarnings)
                    return "";
                StringBuilder sb = new StringBuilder();
                foreach (string warning in Warnings.Values)
                {
                    sb.AppendFormat(warning).AppendLine();
                }
                sb.AppendLine().AppendFormat("Would you like to modify any of these before continuing ?").AppendLine();

                return sb.ToString();
            }
        }

        public string ErrorSTR
        {
            get
            {
                if (!HasErrors)
                    return "";
                StringBuilder sb = new StringBuilder("ERRORS").AppendLine();

                foreach (string error in Errors.Values)
                {
                    sb.AppendFormat(error).AppendLine();
                }
                if (HasWarnings)
                {

                    sb.AppendFormat("WARNINGS").AppendLine();
                    foreach (string warning in Warnings.Values)
                    {
                        sb.AppendFormat(warning).AppendLine();
                    }
                }

                sb.AppendLine().AppendFormat("Please fix these errors before continuing.").AppendLine();
                return sb.ToString();
            }
        }

        internal void Clear(Control ctl)
        {
            if (Errors.ContainsKey(ctl))
                Errors.Remove(ctl);
            if (Warnings.ContainsKey(ctl))
                Warnings.Remove(ctl);
        }

        internal void Clear()
        {
            Errors.Clear();
            Warnings.Clear();
            HasErrors = false;
            HasWarnings = false;
        }
    }
}

