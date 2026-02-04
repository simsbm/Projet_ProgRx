# 🔐 NetAdminPro - Vue d'Ensemble du Système d'Authentification

**Statut:** ✅ Infrastructure Complète | 🚧 Intégration en Cours  
**Date:** Février 2026  
**Version:** 1.0

---

## 📊 Architecture Générale

```
┌─────────────────────────────────────────────────────────────────┐
│                          CLIENT AGENT                            │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  NetworkClient.cs         AuthenticationClient.cs        │  │
│  │  - Gestion connexion TCP  - Gestion tokens              │  │
│  │  - Envoi/réception paquets - Auto-refresh               │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ↑↓
                         (Paquets TCP)
                              ↑↓
┌─────────────────────────────────────────────────────────────────┐
│                          SERVEUR TCP                             │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  TcpServer.cs (gestion connexions)                       │  │
│  │  ├─ Accepte clients                                      │  │
│  │  ├─ Valide tokens d'authentification                    │  │
│  │  ├─ Route vers les services appropriés                  │  │
│  │  └─ Envoie réponses au client                           │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              ↓                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  AuthenticationService.cs                                │  │
│  │  ├─ Authentifier utilisateur (Login)                     │  │
│  │  ├─ Valider tokens JWT                                   │  │
│  │  ├─ Renouveler tokens (Refresh)                          │  │
│  │  ├─ Révoquer tokens (Logout)                             │  │
│  │  ├─ Gérer utilisateurs                                   │  │
│  │  └─ Hash/Verify passwords avec BCrypt                   │  │
│  └──────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  SessionManager.cs                                       │  │
│  │  ├─ Créer sessions authentifiées                         │  │
│  │  ├─ Tracker clients actifs                               │  │
│  │  ├─ Filtrer par rôle/utilisateur                         │  │
│  │  └─ Gérer la durée de vie des sessions                  │  │
│  └──────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  DatabaseInitializer.cs                                  │  │
│  │  ├─ Créer schéma de la BD                                │  │
│  │  ├─ Insérer utilisateurs par défaut                      │  │
│  │  └─ Initialisation sécurisée                             │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                      SQLITE DATABASE                             │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Users                                                  │   │
│  │  ├─ Id, Username, PasswordHash, Email                  │   │
│  │  ├─ Role, IsActive, CreatedAt, LastLoginAt             │   │
│  │  └─ Foreign Keys → AuthTokens, AuditLogs               │   │
│  ├─────────────────────────────────────────────────────────┤   │
│  │  AuthTokens                                             │   │
│  │  ├─ Id, UserId (FK), Token, RefreshToken               │   │
│  │  ├─ IssuedAt, ExpiresAt, RevokedAt                     │   │
│  │  └─ IpAddress, UserAgent                                │   │
│  ├─────────────────────────────────────────────────────────┤   │
│  │  AuditLogs                                              │   │
│  │  ├─ Id, UserId (FK), Action, TargetMachine             │   │
│  │  ├─ Details, Success, Timestamp                         │   │
│  │  └─ Backward compatible (Username field)                │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎯 Flux de Données Principal

### 1️⃣ Connexion Initiale

```
CLIENT                                  SERVER
  │                                       │
  │─────── Packet(Login) ────────────────→│
  │        username + password            │
  │                                       │
  │                              ┌─────────────────┐
  │                              │ AuthService:    │
  │                              │ - Valide pwd    │
  │                              │ - Hash BCrypt   │
  │                              │ - Génère JWT    │
  │                              │ - Crée session  │
  │                              │ - Audit log     │
  │                              └─────────────────┘
  │                                       │
  │←────── Response(LoginResponse) ───────│
  │        token + refreshToken           │
  │        expiresAt + userInfo           │
  │                                       │
  │ StockeTokens()                        │
  │ NotifyUI()                            │
  │                                       │
```

### 2️⃣ Requête Authentifiée

```
CLIENT                                  SERVER
  │                                       │
  │─────── Packet(SystemInfo) ───────────→│
  │        authToken in header            │
  │                                       │
  │                              ┌─────────────────┐
  │                              │ ValideToken():  │
  │                              │ - Vérifie JWT   │
  │                              │ - Cherche en BD │
  │                              │ - Pas révoqué?  │
  │                              │ - Pas expiré?   │
  │                              └─────────────────┘
  │                                       │
  │                         Traite requête
  │                         (AuthTokenValidation)
  │                                       │
  │←────── Response(SystemInfo) ──────────│
  │        données système                │
  │                                       │
```

### 3️⃣ Auto-Refresh Token

```
CLIENT (Timer: toutes les 55 min)       SERVER
  │                                       │
  │─────── RefreshToken Request ────────→│
  │        token + refreshToken           │
  │                                       │
  │                              ┌─────────────────┐
  │                              │ - Valide refresh│
  │                              │ - Révoque ancien│
  │                              │ - Génère nouveau│
  │                              └─────────────────┘
  │                                       │
  │←────── LoginResponse (nouveau) ───────│
  │        newToken + newRefreshToken    │
  │                                       │
  │ UpdateTokens()                        │
  │ Continuer normalement                 │
  │                                       │
```

### 4️⃣ Déconnexion

```
CLIENT                                  SERVER
  │                                       │
  │─────── Packet(Logout) ───────────────→│
  │        authToken                      │
  │                                       │
  │                              ┌─────────────────┐
  │                              │ - Révoque token │
  │                              │ - Ferme session │
  │                              │ - Audit log     │
  │                              └─────────────────┘
  │                                       │
  │←────── LogoutResponse ────────────────│
  │        success                        │
  │                                       │
  │ ClearTokens()                         │
  │ ReturnToLoginScreen()                 │
  │                                       │
```

---

## 🔑 Composants Clés

### **AuthenticationService**
```csharp
public class AuthenticationService
{
    // Authentification
    public LoginResponse Authenticate(LoginRequest, clientIp)
    public AuthTokenValidation ValidateToken(string token)
    public LoginResponse RefreshToken(RefreshTokenRequest, clientIp)
    public bool RevokeToken(string token)
    
    // Gestion utilisateurs
    public bool CreateUser(username, password, email, fullName, role)
    public bool ChangePassword(userId, oldPassword, newPassword)
}
```

### **SessionManager**
```csharp
public class SessionManager
{
    public void CreateSession(clientId, loginResponse, ipAddress)
    public AuthenticatedClientSession GetSession(clientId)
    public bool IsAuthenticated(clientId)
    public void CloseSession(clientId)
    public List<AuthenticatedClientSession> GetActiveSessions()
    public int ActiveSessionCount
}
```

### **AuthenticationClient** (Côté Client)
```csharp
public class AuthenticationClient
{
    public async Task<bool> LoginAsync(username, password)
    public async Task LogoutAsync()
    public async Task<bool> RefreshTokenAsync()
    public void HandleLoginResponse(LoginResponse)
    public NetworkPacket AuthorizePacket(packet)
    public bool IsTokenValid()
}
```

---

## 🔐 Sécurité Implémentée

| Aspect | Implémentation |
|--------|-----------------|
| **Password Hashing** | BCrypt avec salt auto |
| **Token Signing** | HMAC-SHA256 |
| **Token Expiration** | 60 min configurable |
| **Refresh Tokens** | Issus séparés, 7 jours |
| **Token Revocation** | Marca en BD, impossibilité de réutilisation |
| **Session Management** | Tracking IP, UserAgent, temps d'activité |
| **Brute Force Protection** | Délai 1sec sur erreur login |
| **Audit Trail** | Tous les événements loggés |
| **Role-Based Access** | 4 rôles: Admin, Supervisor, Operator, Viewer |

---

## 📁 Structure des Fichiers

```
NetAdminPro/
├── AUTHENTICATION_GUIDE.md          ← Lire d'abord
├── AUTHENTICATION_FLOW.md           ← Diagrammes flux
├── IMPLEMENTATION_CHECKLIST.md      ← Checklist intégration
├── PACKAGE_INSTALLATION.md          ← Installation NuGet
├── SYSTEM_OVERVIEW.md               ← Vous êtes ici
│
├── NetAdmin.Shared/
│   ├── AuthenticationPayload.cs      (LoginRequest, LoginResponse, etc.)
│   └── NetworkPacket.cs             (Mis à jour avec AuthToken)
│
└── NetAdmin.Server/
    ├── Program.cs                   (À mettre à jour)
    ├── appsettings.json             (JWT settings)
    │
    ├── Data/
    │   ├── AppDbContext.cs          (Mis à jour avec Users, AuthTokens)
    │   └── Entities/
    │       ├── User.cs              (Entité utilisateur)
    │       ├── AuthToken.cs         (Tokens JWT + refresh)
    │       ├── AuditLog.cs          (Mis à jour)
    │       └── ClientHost.cs        (Existant)
    │
    └── Services/
        ├── AuthenticationService.cs (Logic principal)
        ├── SessionManager.cs        (Gestion sessions)
        ├── DatabaseInitializer.cs   (Init BD + users par défaut)
        ├── AuthenticationTester.cs  (Suite de tests)
        └── TcpServer.cs             (À intégrer)

└── NetAdmin.Client/
    ├── Program.cs                   (À mettre à jour)
    ├── appsettings.json             (Paramètres client)
    ├── NetworkClient.cs             (À intégrer auth)
    └── AuthenticationClient.cs      (Client-side auth handler)
```

---

## 🚀 Étapes d'Implémentation Rapides

### 1. Installer NuGet Packages
```bash
cd NetAdmin.Server
dotnet add package BCrypt.Net-Next
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.IdentityModel.Tokens
```

### 2. Mettre à jour Program.cs du Serveur
```csharp
var context = new AppDbContext();
var authService = new AuthenticationService(context, "secret-key-32-chars-min");
var sessionManager = new SessionManager();

var initializer = new DatabaseInitializer(context, authService);
initializer.Initialize();
```

### 3. Intégrer à TcpServer
- Ajouter AuthenticationService
- Traiter PacketType.Login
- Valider tokens pour requêtes sensibles

### 4. Intégrer au Client
- Créer AuthenticationClient
- Implémenter screen de login
- Auto-refresh token

### 5. Compiler et Tester
```bash
dotnet build
dotnet run
```

---

## 📊 Utilisateurs Par Défaut

```
┌───────────┬──────────────────┬──────────────────┐
│ Username  │ Password         │ Role             │
├───────────┼──────────────────┼──────────────────┤
│ admin     │ Admin@123!       │ Administrator    │
│ supervisor│ Supervisor@123!  │ Supervisor       │
│ operator  │ Operator@123!    │ Operator         │
│ viewer    │ Viewer@123!      │ Viewer           │
└───────────┴──────────────────┴──────────────────┘
```

**⚠️ À CHANGER EN PRODUCTION!**

---

## 🧪 Test Rapide

Après l'installation, tester avec:

```bash
# Serveur
dotnet run --project NetAdmin.Server

# Dans une autre console
dotnet run --project NetAdmin.Client
```

La suite de tests s'exécutera automatiquement.

---

## 📞 Points de Contact

### Si Erreur de Compilation:
1. Vérifier les packages NuGet sont installés
2. Lire `PACKAGE_INSTALLATION.md`
3. `dotnet clean` puis `dotnet build`

### Si Erreur d'Authentification:
1. Vérifier secret JWT en appsettings.json
2. Vérifier la base de données existe
3. Lire `AUTHENTICATION_FLOW.md`

### Si Erreur de Connexion:
1. Vérifier serveur écoute port 5000
2. Vérifier firewall
3. Consulter logs serveur

---

## 📚 Lectures Recommandées

1. **AUTHENTICATION_GUIDE.md** - Comprendre l'architecture
2. **AUTHENTICATION_FLOW.md** - Voir diagrammes flux
3. **IMPLEMENTATION_CHECKLIST.md** - Suivre étapes
4. **Code source AuthenticationService.cs** - Détails impl.

---

## ✅ Checklist Démarrage

- [ ] Lire AUTHENTICATION_GUIDE.md
- [ ] Installer NuGet packages
- [ ] Mettre à jour Program.cs serveur
- [ ] Intégrer AuthenticationService au TcpServer
- [ ] Ajouter AuthenticationClient au NetworkClient
- [ ] Compiler sans erreurs
- [ ] Tester login avec admin/Admin@123!
- [ ] Tester requête authentifiée (SystemInfo)
- [ ] Tester logout
- [ ] Tester refresh token

---

**Créé:** Février 2026  
**Version:** 1.0  
**Prêt pour:** Développement & Tests  
**Production:** À sécuriser davantage
