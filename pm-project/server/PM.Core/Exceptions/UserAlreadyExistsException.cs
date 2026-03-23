namespace PM.Core.Exceptions
{
    public class UserAlreadyExistsException : Exception
    {
        public UserAlreadyExistsException(string username)
            : base($"User with username '{username}' already exists.")
        {
        }
    }
}