using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using NetAdmin.Server.Data;
using NetAdmin.Server.Data.Entities;
using NetAdmin.Shared;
using BCrypt.Net;

namespace NetAdmin.Server.Services
{
    public class AuthenticationService
    {
        private readonly AppDbContext _context;
        private readonly string _jwtSecret;
        private readonly int _tokenExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        public AuthenticationService(AppDbContext context, string jwtSecret, int tokenExpirationMinutes = 60, int refreshTokenExpirationDays = 7)
        {
            _context = context;
            _jwtSecret = jwtSecret ?? throw new ArgumentNullException(nameof(jwtSecret), "JWT secret cannot be null");
            _tokenExpirationMinutes = tokenExpirationMinutes;
            _refreshTokenExpirationDays = refreshTokenExpirationDays;
        }

        /// <summary>
        /// Authentifie un utilisateur et génère les tokens
        /// </summary>
        public LoginResponse Authenticate(LoginRequest request, string clientIp, string? userAgent = null)
        {
            try
            {
                // Valider les paramètres
                if (string.IsNullOrWhiteSpace(request?.Username) || string.IsNullOrWhiteSpace(request?.Password))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Identifiant ou mot de passe vide"
                    };
                }

                // Chercher l'utilisateur
                var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);
                if (user == null || !user.IsActive)
                {
                    // Délai intentionnel pour prévenir brute force
                    System.Threading.Thread.Sleep(1000);
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Identifiant ou mot de passe incorrect"
                    };
                }

                // Vérifier le mot de passe
                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    System.Threading.Thread.Sleep(1000);
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Identifiant ou mot de passe incorrect"
                    };
                }

                // Générer les tokens
                var (token, refreshToken, expiresAt) = GenerateTokens(user);

                // Sauvegarder le token dans la base de données
                var authToken = new AuthToken
                {
                    UserId = user.Id,
                    Token = token,
                    RefreshToken = refreshToken,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    IpAddress = clientIp,
                    UserAgent = userAgent ?? ""
                };

                _context.AuthTokens.Add(authToken);
                user.LastLoginAt = DateTime.UtcNow;
                _context.SaveChanges();

                return new LoginResponse
                {
                    Success = true,
                    Message = "Connexion réussie",
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    UserId = user.Id,
                    Username = user.Username,
                    Role = user.Role.ToString()
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Erreur serveur: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Valide un token JWT
        /// </summary>
        public AuthTokenValidation ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
                var username = jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value;
                var role = jwtToken.Claims.First(x => x.Type == ClaimTypes.Role).Value;

                // Vérifier si le token n'est pas révoqué dans la base de données
                var dbToken = _context.AuthTokens.FirstOrDefault(t => t.Token == token && !t.IsRevoked && !t.IsExpired);
                if (dbToken == null)
                {
                    return new AuthTokenValidation
                    {
                        IsValid = false,
                        ErrorMessage = "Token révoqué ou expiré dans la base de données"
                    };
                }

                return new AuthTokenValidation
                {
                    IsValid = true,
                    UserId = userId,
                    Username = username,
                    Role = role
                };
            }
            catch (Exception ex)
            {
                return new AuthTokenValidation
                {
                    IsValid = false,
                    ErrorMessage = $"Validation échouée: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Renouvelle un token expiré avec le refresh token
        /// </summary>
        public LoginResponse RefreshToken(RefreshTokenRequest request, string clientIp)
        {
            try
            {
                // Valider le format du refresh token (basique)
                if (string.IsNullOrWhiteSpace(request?.RefreshToken))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Refresh token invalide"
                    };
                }

                // Chercher le token dans la base de données
                var dbToken = _context.AuthTokens
                    .Include(t => t.User)
                    .FirstOrDefault(t => t.RefreshToken == request.RefreshToken && !t.IsRevoked);

                if (dbToken == null || dbToken.IsExpired)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Refresh token invalide ou expiré"
                    };
                }

                var user = dbToken.User;
                if (!user.IsActive)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Utilisateur inactif"
                    };
                }

                // Générer nouveaux tokens
                var (newToken, newRefreshToken, expiresAt) = GenerateTokens(user);

                // Révoquer l'ancien token
                dbToken.RevokedAt = DateTime.UtcNow;

                // Créer le nouveau token
                var authToken = new AuthToken
                {
                    UserId = user.Id,
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    IpAddress = clientIp
                };

                _context.AuthTokens.Add(authToken);
                _context.SaveChanges();

                return new LoginResponse
                {
                    Success = true,
                    Message = "Token renouvelé avec succès",
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = expiresAt,
                    UserId = user.Id,
                    Username = user.Username,
                    Role = user.Role.ToString()
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Erreur lors du renouvellement: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Révoque un token (déconnexion)
        /// </summary>
        public bool RevokeToken(string token)
        {
            try
            {
                var dbToken = _context.AuthTokens.FirstOrDefault(t => t.Token == token);
                if (dbToken == null)
                    return false;

                dbToken.RevokedAt = DateTime.UtcNow;
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Crée un nouvel utilisateur
        /// </summary>
        public bool CreateUser(string username, string password, string email, string fullName, UserRole role)
        {
            try
            {
                // Vérifier que l'utilisateur n'existe pas déjà
                if (_context.Users.Any(u => u.Username == username))
                    return false;

                var user = new User
                {
                    Username = username,
                    PasswordHash = HashPassword(password),
                    Email = email,
                    FullName = fullName,
                    Role = role,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Change le mot de passe d'un utilisateur
        /// </summary>
        public bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null)
                    return false;

                if (!VerifyPassword(oldPassword, user.PasswordHash))
                    return false;

                user.PasswordHash = HashPassword(newPassword);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Méthodes privées

        private (string token, string refreshToken, DateTime expiresAt) GenerateTokens(User user)
        {
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var expiresAt = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("FullName", user.FullName ?? "")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Refresh token simple: guid + timestamp
            var refreshToken = $"{Guid.NewGuid()}_{DateTime.UtcNow.Ticks}";

            return (tokenString, refreshToken, expiresAt);
        }

        private string HashPassword(string password)
        {
            // Utiliser BCrypt pour hasher le mot de passe
            return BCrypt.Net.BCrypt.HashPassword(password, 10);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
