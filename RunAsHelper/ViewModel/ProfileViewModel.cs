using System.Linq;
using GalaSoft.MvvmLight;

namespace RunAsHelper.ViewModel
{
    public class ProfileViewModel : ViewModelBase
    {
        private string _username;
        private string _domain;
        private bool _passwordConfimed;

        public string Username
        {
            get => _username;
            set
            {
                SetDomainIfFound(value);
                _username = value;
            }
        }

        public string GetUsernameWithoutDomain()
        {
            var data = Username.Split('/', '\\', '@');
            if (Username.Contains("@"))
            {
                return data.First();
            }

            return data.First();
        }

        private void SetDomainIfFound(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var data = value.Split('/', '\\', '@');
                if (value.Contains("@"))
                {
                    Domain = data.Last();
                }
                else if (data.Length > 1)
                {
                    Domain = data.First();
                }
            }
        }

        public string Domain
        {
            get => _domain;
            set
            {
                _domain = value;
                
                RaisePropertyChanged(nameof(Domain));
            }
        }

        public string EncryptedPassword { get; set; }

        public string ProfileName => _username;

        public bool PasswordConfimed
        {
            get => _passwordConfimed;
            set
            {
                _passwordConfimed = value;
                RaisePropertyChanged(nameof(PasswordConfimed));
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ProfileViewModel)) return false;
            var incoming = obj as ProfileViewModel;

            if (Username != incoming.Username) return false;
            if (EncryptedPassword != incoming.EncryptedPassword) return false;
            if (Domain != incoming.Domain) return false;

            return true;
        }

        public override int GetHashCode()
        {
            var uHash = string.IsNullOrEmpty(Username) ? string.Empty.GetHashCode() : Username.GetHashCode();
            var pHash = string.IsNullOrEmpty(EncryptedPassword) ? string.Empty.GetHashCode() : EncryptedPassword.GetHashCode();
            var dHash = string.IsNullOrEmpty(Domain) ? string.Empty.GetHashCode() : Domain.GetHashCode();

            return uHash ^ pHash ^ dHash;
        }
    }
}