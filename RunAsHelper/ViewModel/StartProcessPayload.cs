namespace RunAsHelper.ViewModel
{
    internal class StartProcessPayload
    {
        public string Domain { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Path { get; set; }
    }
}