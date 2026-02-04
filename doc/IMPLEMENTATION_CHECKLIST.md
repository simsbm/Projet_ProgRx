# 🔐 Système d'Authentification NetAdminPro - Implantation Complète

**Version:** 1.0  
**Date:** Février 2026  
**Statut:** Infrastructure créée ✅ | Intégration en cours 🚧

---

## 📦 Fichiers Créés

### Infrastructure Authentification

| Fichier | Purpose | Priorité |
|---------|---------|----------|
| `User.cs` | Entité utilisateur avec roles | ⭐⭐⭐ |
| `AuthToken.cs` | Stockage JWT + refresh tokens | ⭐⭐⭐ |
| `AuthenticationPayload.cs` | Modèles requête/réponse | ⭐⭐⭐ |
| `AuthenticationService.cs` | Logic d'authentification + JWT | ⭐⭐⭐ |
| `SessionManager.cs` | Gestion des sessions actives | ⭐⭐⭐ |
| `DatabaseInitializer.cs` | Init DB + users par défaut | ⭐⭐ |
| `AuthenticationClient.cs` | Client-side auth handler | ⭐⭐ |
| `appsettings.json` (x2) | Configuration JWT + client | ⭐⭐ |

### Documentation

| Fichier | Contenu |
|---------|---------|
| `AUTHENTICATION_GUIDE.md` | Guide complet d'intégration |
| `AUTHENTICATION_FLOW.md` | Diagrammes et flux détaillés |
| `IMPLEMENTATION_CHECKLIST.md` | **← LISEZ CELUI-CI** |

---

## ⚡ Démarrage Rapide (5 minutes)

### 1. Ajouter les NuGet packages

```powershell
cd NetAdmin.Server
dotnet add package BCrypt.Net-Next
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.IdentityModel.Tokens
```

### 2. Mettre à jour `Program.cs` du serveur

```csharp
using NetAdmin.Server.Data;
using NetAdmin.Server.Services;
using Microsoft.EntityFrameworkCore;

// Initialiser la DB
using (var context = new AppDbContext())
{
    context.Database.EnsureCreated();
}

// Créer les services
var context = new AppDbContext();
var authService = new AuthenticationService(
    context,
    "change-me-to-32-char-secret-key!!!",
    tokenExpirationMinutes: 60,
    refreshTokenExpirationDays: 7
);

// Initialiser avec utilisateurs par défaut
var initializer = new DatabaseInitializer(context, authService);
initializer.Initialize();

var sessionManager = new SessionManager();

// Créer le serveur TCP avec support auth
var server = new TcpServer(5000);
server.Start();

Console.ReadLine();
server.Stop();
```

### 3. Compiler et tester

```bash
cd NetAdmin.Server
dotnet build
dotnet run
```

**Résultat attendu:**
```
[DB] Initialisation de la base de données...
[DB] Utilisateurs par défaut créés avec succès!
[DB] Identifiants disponibles:
     - admin / Admin@123!
     - supervisor / Supervisor@123!
     - operator / Operator@123!
     - viewer / Viewer@123!
[HH:mm:ss] Serveur démarré sur le port 127.0.0.1:5000
```

---

## 📋 Checklist d'Implémentation Complète

### Phase 1: Infrastructure de Base ✅ FAIT

- [x] Créer entité `User`
- [x] Créer entité `AuthToken`
- [x] Créer `AuthenticationService` avec JWT
- [x] Créer `SessionManager`
- [x] Créer `DatabaseInitializer`
- [x] Mettre à jour `AppDbContext`
- [x] Créer modèles API (LoginRequest, etc.)

### Phase 2: Intégration au Serveur TCP 🚧 EN COURS

À implémenter dans `TcpServer.cs`:

```csharp
// 1. Ajouter les champs
private AuthenticationService _authService;
private SessionManager _sessionManager;

// 2. Modifier HandleClientAsync pour traiter les Login
if (packet.Type == PacketType.Login)
{
    var loginRequest = packet.DeserializePayload<LoginRequest>();
    var response = _authService.Authenticate(loginRequest, clientEndPoint);
    
    if (response.Success)
    {
        _sessionManager.CreateSession(clientEndPoint, response, clientEndPoint);
    }
    
    var responsePacket = NetworkPacket.Create(
        PacketType.LoginResponse,
        "SERVER",
        response
    );
    await SendToClient(clientEndPoint, responsePacket);
}

// 3. Valider les requêtes authentifiées
private bool RequiresAuthentication(PacketType type) =>
    type switch
    {
        PacketType.SystemInfo => true,
        PacketType.Command => true,
        PacketType.ProcessList => true,
        _ => false
    };

// 4. Vérifier le token
if (RequiresAuthentication(packet.Type))
{
    if (string.IsNullOrEmpty(packet.AuthToken))
    {
        SendError(clientEndPoint, "Authentication requise");
        return;
    }
    
    var validation = _authService.ValidateToken(packet.AuthToken);
    if (!validation.IsValid)
    {
        SendError(clientEndPoint, validation.ErrorMessage);
        return;
    }
}
```

### Phase 3: Intégration au Client 🔜 À FAIRE

Dans `NetworkClient.cs`:

```csharp
private AuthenticationClient _authClient;

public async Task InitializeAuthentication(string username, string password)
{
    _authClient = new AuthenticationClient(this);
    await _authClient.LoginAsync(username, password);
}

// Auto-refresh token toutes les 5 minutes
private async Task TokenRefreshLoopAsync()
{
    while (IsConnected)
    {
        await Task.Delay(TimeSpan.FromMinutes(5));
        await _authClient?.RefreshTokenAsync();
    }
}
```

### Phase 4: Interface Utilisateur 🔜 À FAIRE

Créer une fenêtre de connexion dans `MainWindow.xaml`:

```xaml
<StackPanel>
    <TextBlock Text="NetAdmin Pro - Connexion" FontSize="20" FontWeight="Bold"/>
    <TextBox x:Name="UsernameTextBox" PlaceholderText="Nom d'utilisateur"/>
    <PasswordBox x:Name="PasswordBox" PlaceholderText="Mot de passe"/>
    <Button Content="Connexion" Click="LoginButton_Click"/>
</StackPanel>
```

---

## 🧪 Tests à Faire

### Test 1: Authentification Valide
```bash
# Login avec credentials valides
Username: admin
Password: Admin@123!
Expected: Success + Token reçu
```

### Test 2: Authentification Invalide
```bash
Username: admin
Password: WrongPassword
Expected: Erreur "Identifiant ou mot de passe incorrect"
```

### Test 3: Token Refresh
```bash
# Attendre 55 minutes ou forcer expiration
Expected: Nouveau token généré, session toujours active
```

### Test 4: Requête Protégée
```bash
# Envoyer SystemInfo SANS token
Expected: Erreur "Authentication requise"

# Envoyer SystemInfo AVEC token valide
Expected: Succès, données reçues
```

### Test 5: Logout
```bash
# Logout puis essayer une requête
Expected: Erreur "Token révoqué"
```

---

## 🔑 Secrets à Changer

### En Développement
```json
"Secret": "dev-secret-key-change-in-production!!!"
```

### En Production
```json
"Secret": "generate-with-SecureRandom-32-char-min"
```

**Générer une clé sécurisée:**
```csharp
using System.Security.Cryptography;

var key = new byte[32];
using (var rng = RandomNumberGenerator.Create())
{
    rng.GetBytes(key);
}
string secret = Convert.ToBase64String(key);
Console.WriteLine(secret);
```

---

## 📚 Documentation Complète

Lire dans cet ordre:

1. **`AUTHENTICATION_GUIDE.md`** - Vue d'ensemble
2. **`AUTHENTICATION_FLOW.md`** - Flux détaillés
3. **`IMPLEMENTATION_CHECKLIST.md`** - ← Vous êtes ici
4. Code source des services

---

## 🆘 Dépannage

### Erreur: "Token de JWT invalide"
- Vérifier que le secret JWT est le même côté client/serveur
- Vérifier l'expiration du token

### Erreur: "Utilisateur pas trouvé"
- Vérifier que la base de données est initialisée
- Vérifier le fichier `netadmin.db`

### Erreur: "Package BCrypt not found"
- Relancer: `dotnet add package BCrypt.Net-Next`

### Erreur: "Connection refused"
- Vérifier que le serveur écoute sur port 5000
- Vérifier que firewall n'est pas bloquant

---

## 📞 Support

Pour les problèmes:
1. Vérifier les logs serveur
2. Lire AUTHENTICATION_FLOW.md
3. Consulter le code source

---

## 🚀 Prochaines Étapes Avancées

- [ ] 2FA (Two-Factor Authentication)
- [ ] OAuth2 / OpenID Connect
- [ ] Rate limiting
- [ ] Audit logging avancé
- [ ] Certificate-based auth
- [ ] AD/LDAP integration

---

**Créé:** Février 2026  
**Dernière modification:** Février 2026  
**Auteur:** GitHub Copilot
