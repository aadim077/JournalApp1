using JournalApp.Core.Entities;
using JournalApp.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace JournalApp.Core.Services;


// Service for user authentication and authorization.
// Handles user registration, login, and password management.

public class AuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Streak> _streakRepository;
    private User? _currentUser;

    public AuthService(IRepository<User> userRepository, IRepository<Streak> streakRepository)
    {
        _userRepository = userRepository;
        _streakRepository = streakRepository;
    }

    public User? CurrentUser => _currentUser;
    public bool IsPinVerified { get; private set; }

    public async Task<(bool Success, string Message)> SetPinAsync(string pin)
    {
        if (_currentUser == null) return (false, "Not authenticated.");
        if (string.IsNullOrWhiteSpace(pin) || pin.Length != 4)
            return (false, "PIN must be 4 digits.");

        var salt = GenerateSalt();
        var hash = HashPassword(pin, salt);

        _currentUser.PinHash = hash;
        _currentUser.PinSalt = salt;

        await _userRepository.UpdateAsync(_currentUser);
        await _userRepository.SaveChangesAsync();

        IsPinVerified = true;
        return (true, "PIN set successfully.");
    }

    public async Task<(bool Success, string Message)> VerifyPinAsync(string pin)
    {
        if (_currentUser == null) return (false, "Not authenticated.");
        if (string.IsNullOrEmpty(_currentUser.PinHash)) return (false, "PIN not set up.");

        var hash = HashPassword(pin, _currentUser.PinSalt!);
        if (hash == _currentUser.PinHash)
        {
            IsPinVerified = true;
            return (true, "PIN verified.");
        }

        return (false, "Incorrect PIN.");
    }

   
    //Registers a new user with username and password.
    
    public async Task<(bool Success, string Message)> RegisterAsync(string username, string password)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                return (false, "Username must be at least 3 characters long.");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return (false, "Password must be at least 6 characters long.");

            // Check if username already exists
            var existingUsers = await _userRepository.FindAsync(u => u.Username == username);
            if (existingUsers.Any())
                return (false, "Username already exists.");

            // Create password hash
            var salt = GenerateSalt();
            var passwordHash = HashPassword(password, salt);

            // Create new user
            var user = new User
            {
                Username = username,
                PasswordHash = passwordHash,
                Salt = salt,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // Initialize streak for new user
            var streak = new Streak
            {
                UserId = user.Id,
                CurrentStreak = 0,
                LongestStreak = 0,
                UpdatedAt = DateTime.UtcNow
            };

            await _streakRepository.AddAsync(streak);
            await _streakRepository.SaveChangesAsync();

            return (true, "Registration successful!");
        }
        catch (Exception ex)
        {
            return (false, $"Registration failed: {ex.Message}");
        }
    }

    
    /// Authenticates a user with username and password.
    
    public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
    {
        try
        {
            var users = await _userRepository.FindAsync(u => u.Username == username);
            var user = users.FirstOrDefault();

            if (user == null)
                return (false, "Invalid username or password.");

            var passwordHash = HashPassword(password, user.Salt);

            if (passwordHash != user.PasswordHash)
                return (false, "Invalid username or password.");

            _currentUser = user;
            return (true, "Login successful!");
        }
        catch (Exception ex)
        {
            return (false, $"Login failed: {ex.Message}");
        }
    }

    
    // Logs out the current user.
    
    public void Logout()
    {
        _currentUser = null;
        IsPinVerified = false;
    }

   
    // Generates a random salt for password hashing.
    
    private string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }

   
    // Hashes a password with the given salt using PBKDF2.
    
    private string HashPassword(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256))
        {
            var hashBytes = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
