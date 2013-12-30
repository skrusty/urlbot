namespace BenBOT
{
    public class SMTPSettings
    {
        public string SMTPHost { get; set; }
        public int SMTPPort { get; set; }
        public string SMTPUsername { get; set; }
        public string SMTPPassword { get; set; }

        // Can differ from username is SMTP Server allows
        public string DefaultEmailAddress { get; set; }
    }
}