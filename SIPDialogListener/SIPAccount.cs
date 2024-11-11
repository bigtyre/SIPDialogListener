namespace BigTyre.Phones
{
    public class SIPAccount
    {
        public SIPAccount(string username, string extension, string password)
        {
            Username = username;
            Extension = extension;
            Password = password;
        }

        public string Username { get; }
        public string Extension { get; }
        public string Password { get; }
    }

}
