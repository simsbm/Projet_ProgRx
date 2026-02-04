# Système d'Authentification NetAdminPro - Guide d'Intégration

## 📋 Vue d'ensemble

Un système d'authentification complet a été mis en place avec:
- **JWT (JSON Web Tokens)** pour l'authentification sans état
- **BCrypt** pour le hachage sécurisé des mots de passe
- **Refresh tokens** pour renouveler les sessions
- **Gestion des sessions** côté serveur
- **Audit trail** pour tracer les actions

## 🎯 Composants Créés

### 1. **Entités de Base de Données**

#### `User.cs`
- Stocke les utilisateurs avec rôles (Administrator, Supervisor, Operator, Viewer)
- Hash BCrypt du mot de passe
- Statut actif/inactif
- Historique de connexion

#### `AuthToken.cs`
- JWT tokens avec expiration
- Refresh tokens
- Révocation (logout)
- Tracking IP et User Agent

#### `AuditLog.cs` (mis à jour)
- Relation avec l'utilisateur qui a effectué l'action
- Backward compatible avec le champ Username

### 2. **Services**

#### `AuthenticationService.cs`
```csharp
// Méthodes principales:
- Authenticate(LoginRequest) → LoginResponse
- ValidateToken(string token) → AuthTokenValidation
- RefreshToken(RefreshTokenRequest) → LoginResponse
- RevokeToken(string token) → bool
- CreateUser(...) → bool
- ChangePassword(userId, oldPassword, newPassword) → bool
```

#### `SessionManager.cs`
- Gestion des sessions actives
- Tracking des clients authentifiés
- Événements de connexion/déconnexion
- Filtrage par rôle ou utilisateur

#### `DatabaseInitializer.cs`
- Création des tables
- Utilisateurs par défaut (4 rôles)
- Réinitialisation sécurisée

### 3. **Modèles de Requête/Réponse**

#### `AuthenticationPayload.cs`
- `LoginRequest` - Pour la connexion
- `LoginResponse` - Réponse avec token
- `RefreshTokenRequest` - Pour renouvellement
- `AuthTokenValidation` - Pour validation

## 🔧 Étapes d'Intégration Restantes

### Étape 1: Mettre à jour les dépendances du projet Server

Ajouter les packages NuGet:
```bash
dotnet add NetAdmin.Server package BCrypt.Net-Next
dotnet add NetAdmin.Server package System.IdentityModel.Tokens.Jwt
```

### Étape 2: Mettre à jour le Program.cs du serveur

```csharp
using Microsoft.EntityFrameworkCore;
using NetAdmin.Server.Data;
using NetAdmin.Server.Services;

// Configuration de la base de données
var context = new AppDbContext();
var authService = new AuthenticationService(
    context, 
    "your-super-secret-key-min-32-characters-for-security",
    tokenExpirationMinutes: 60,
    refreshTokenExpirationDays: 7
);

// Initialisation de la base de données
var initializer = new DatabaseInitializer(context, authService);
initializer.Initialize();

// Créer les services
var sessionManager = new SessionManager();
var tcpServer = new TcpServer(5000);

// Abonner aux événements
tcpServer.OnPacketReceived += (packet) => HandlePacket(packet, authService, sessionManager);

tcpServer.Start();
Console.ReadLine();
tcpServer.Stop();
```

### Étape 3: Implémenter la logique d'authentification dans TcpServer

Modifier `HandleClientAsync` pour traiter les paquets de connexion:

```csharp
if (packet.Type == PacketType.Login)
{
    var loginRequest = packet.DeserializePayload<LoginRequest>();
    var response = authService.Authenticate(loginRequest, clientEndPoint);
    
    if (response.Success)
    {
        // Créer une session
        sessionManager.CreateSession(packet.ClientId ?? clientEndPoint, response, clientEndPoint);
    }
    
    // Renvoyer la réponse au client
    var responsePacket = NetworkPacket.Create(
        PacketType.LoginResponse, 
        "SERVER", 
        response
    );
    await SendToClient(clientEndPoint, responsePacket);
}
```

### Étape 4: Protéger les requêtes authentifiées

Ajouter une validation dans `HandleClientAsync`:

```csharp
// Vérifier que le client est authentifié pour les requêtes sensibles
if (RequiresAuthentication(packet.Type))
{
    if (string.IsNullOrEmpty(packet.AuthToken))
    {
        SendError(clientEndPoint, "Token d'authentification requis");
        return;
    }
    
    var validation = authService.ValidateToken(packet.AuthToken);
    if (!validation.IsValid)
    {
        SendError(clientEndPoint, "Token invalide");
        return;
    }
}
```

### Étape 5: Mettre à jour le Client

Implémenter la connexion dans `NetworkClient.cs`:

```csharp
public async Task LoginAsync(string username, string password)
{
    var loginRequest = new LoginRequest 
    { 
        Username = username, 
        Password = password 
    };
    
    var packet = NetworkPacket.Create(PacketType.Login, _machineName, loginRequest);
    await SendPacket(packet);
    
    // Attendre la réponse...
}
```

## 🔐 Sécurité

### Bonnes pratiques implémentées:
1. ✅ Hash BCrypt avec salt
2. ✅ JWT avec signature HMAC-SHA256
3. ✅ Délai intentionnel sur erreur (brute force protection)
4. ✅ Révocation de tokens (audit trail)
5. ✅ Expiration configurable
6. ✅ Refresh tokens séparés

### À faire:
- [ ] Changer le JWT secret en production
- [ ] Utiliser HTTPS pour les connexions
- [ ] Implémenter 2FA si nécessaire
- [ ] Ajouter rate limiting
- [ ] Activer CORS sécurisé

## 📊 Base de Données

Migrations automatiques via `EnsureCreated()`:

```
Users
├─ Id (PK)
├─ Username (UNIQUE)
├─ PasswordHash
├─ Email (UNIQUE)
├─ Role (Enum)
├─ IsActive
├─ LastLoginAt
└─ ...

AuthTokens
├─ Id (PK)
├─ UserId (FK)
├─ Token
├─ RefreshToken
├─ ExpiresAt
├─ RevokedAt
└─ ...

AuditLog
├─ Id (PK)
├─ UserId (FK) - Nullable
├─ Action
├─ Success
└─ ...
```

## 🎓 Utilisateurs Par Défaut

| Username | Password | Rôle |
|----------|----------|------|
| admin | Admin@123! | Administrator |
| supervisor | Supervisor@123! | Supervisor |
| operator | Operator@123! | Operator |
| viewer | Viewer@123! | Viewer |

## 📝 Prochaines Étapes

1. Ajouter les packages NuGet
2. Mettre à jour Program.cs
3. Intégrer au TcpServer
4. Tester avec le client
5. Implémenter interface de connexion (UI)

---

**Version:** 1.0  
**Date:** Février 2026
