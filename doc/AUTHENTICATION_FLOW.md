# Flux d'Authentification NetAdminPro

## 🔄 Diagramme Général

```
CLIENT                              SERVER
  |                                   |
  |------- LoginRequest ----->         |
  |   (username + password)            |
  |                                    |
  |                            Valide credentials
  |                            Génère JWT Token
  |                            Crée AuthToken en DB
  |                                    |
  |<----- LoginResponse -----          |
  |  (token + refreshToken)            |
  |                                    |
  |  Stocke les tokens localement     |
  |  S'authentifie en tant qu'user   |
  |                                    |
  |------- SystemInfo ----->           |
  |   (+ AuthToken)                    |
  |                                    |
  |                            Valide le token
  |                            Traite la requête
  |                                    |
  |<----- SystemInfo Response -        |
  |   (données + métadonnées)          |
  |                                    |
  |  ... (plus de requêtes)            |
  |  ... (token en header)             |
  |                                    |
  |------- Logout ----->               |
  |   (+ AuthToken)                    |
  |                                    |
  |                            Révoque le token
  |                            Ferme la session
  |                                    |
  |<----- LogoutResponse -----         |
```

## 📝 Flux Détaillé

### 1️⃣ **CONNEXION (Login)**

#### Requête Client → Serveur

```json
{
  "Type": "Login",
  "SenderId": "CLIENT",
  "Timestamp": "2026-02-04T10:30:00Z",
  "ClientId": "unique-client-id",
  "PayloadJson": {
    "Username": "admin",
    "Password": "Admin@123!"
  },
  "AuthToken": null
}
```

#### Traitement Serveur

1. Valide username + password
2. Hash le password avec BCrypt
3. Compare avec la base de données
4. Si OK:
   - Génère un JWT Token (30 minutes par défaut)
   - Génère un Refresh Token
   - Crée un AuthToken dans la base de données
   - Crée une session dans SessionManager

#### Réponse Serveur → Client

```json
{
  "Success": true,
  "Message": "Connexion réussie",
  "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "RefreshToken": "550e8400-e29b-41d4-a716-446655440000_637000000000",
  "ExpiresAt": "2026-02-04T11:00:00Z",
  "UserId": 1,
  "Username": "admin",
  "Role": "Administrator"
}
```

### 2️⃣ **REQUÊTE AUTHENTIFIÉE (Secured Request)**

Toute requête après login doit inclure le token:

```json
{
  "Type": "SystemInfo",
  "SenderId": "LAPTOP-ABC123",
  "Timestamp": "2026-02-04T10:35:00Z",
  "ClientId": "unique-client-id",
  "PayloadJson": { ... },
  "AuthToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### Validation Serveur

1. Vérifie la signature JWT
2. Vérifie l'expiration
3. Cherche le token dans la base de données
4. Vérifie qu'il n'a pas été révoqué
5. Extrait les informations de l'utilisateur
6. Traite la requête

### 3️⃣ **RENOUVELLEMENT DU TOKEN (Token Refresh)**

Quand le token approche de l'expiration:

#### Requête Client → Serveur

```json
{
  "Type": "RefreshToken",
  "SenderId": "CLIENT",
  "PayloadJson": {
    "Token": "ancien-jwt-token",
    "RefreshToken": "550e8400-e29b-41d4-a716-446655440000_637000000000"
  }
}
```

#### Traitement Serveur

1. Valide le RefreshToken dans la DB
2. Vérifie qu'il n'est pas expiré
3. Révoque l'ancien token
4. Génère un nouveau JWT Token
5. Génère un nouveau Refresh Token
6. Met à jour la session

#### Réponse Serveur → Client

```json
{
  "Success": true,
  "Message": "Token renouvelé avec succès",
  "Token": "nouveau-jwt-token",
  "RefreshToken": "nouveau-refresh-token",
  "ExpiresAt": "2026-02-04T11:30:00Z",
  ...
}
```

### 4️⃣ **DÉCONNEXION (Logout)**

#### Requête Client → Serveur

```json
{
  "Type": "Logout",
  "SenderId": "CLIENT",
  "PayloadJson": {},
  "AuthToken": "jwt-token-a-revoquer"
}
```

#### Traitement Serveur

1. Valide le token
2. Marque le token comme révoqué (RevokedAt = NOW)
3. Ferme la session dans SessionManager
4. Envoie la confirmation

#### Réponse Serveur → Client

```json
{
  "Type": "LogoutResponse",
  "PayloadJson": {
    "Success": true,
    "Message": "Déconnexion réussie"
  }
}
```

#### Nettoyage Client

1. Efface le token en mémoire
2. Efface le refresh token
3. Réinitialise l'état d'authentification

---

## 🔐 **Sécurité du Flux**

### ✅ Mesures Implémentées

1. **JWT Signing**
   - Signature HMAC-SHA256
   - Secret robuste (min. 32 caractères)

2. **Password Security**
   - Hash BCrypt avec salt
   - Délai intentionnel sur erreur (1 sec)
   - Prévention brute force

3. **Token Management**
   - Expiration configurable
   - Révocation possible (logout)
   - Refresh token séparé
   - Validation de signature

4. **Session Management**
   - Session en mémoire + DB
   - Tracking IP et UserAgent
   - Fermeture propre

### ⚠️ À Mettre en Œuvre en Production

1. **Transport**
   - [ ] HTTPS/TLS obligatoire
   - [ ] Certificate pinning

2. **Tokens**
   - [ ] Stocker tokens en secure storage (pas en mémoire)
   - [ ] Clear tokens on app exit

3. **Rate Limiting**
   - [ ] Limiter tentatives de login
   - [ ] Rate limit par IP

4. **Audit**
   - [ ] Logger toutes les tentatives
   - [ ] Alertes sur échecss répétés

5. **2FA (Optional)**
   - [ ] SMS/Email verification
   - [ ] TOTP support

---

## 📊 **Durée de vie des Tokens**

| Token | Durée | Purpose |
|-------|-------|---------|
| JWT Access Token | 60 min | Authentifier les requêtes |
| Refresh Token | 7 jours | Renouveler JWT sans se reconnecter |
| Session | Durée login | Tracer la session active |

---

## 🛠️ **Exemples de Code**

### Côté Client

```csharp
// Connexion
var authClient = new AuthenticationClient(networkClient);
bool success = await authClient.LoginAsync("admin", "Admin@123!");

if (success)
{
    // Envoyer une requête protégée
    var packet = NetworkPacket.Create(PacketType.SystemInfo, "LAPTOP-123", ...);
    var authenticatedPacket = authClient.AuthorizePacket(packet);
    await networkClient.SendPacket(authenticatedPacket);
}

// Renouvellement automatique
await authClient.RefreshTokenAsync();

// Déconnexion
await authClient.LogoutAsync();
```

### Côté Serveur

```csharp
// Validation du token
var validation = authService.ValidateToken(packet.AuthToken);
if (!validation.IsValid)
{
    SendError("Token invalide");
    return;
}

// Traiter la requête en tant qu'utilisateur validé
Console.WriteLine($"Requête de {validation.Username} ({validation.Role})");

// Créer un audit log
var auditLog = new AuditLog
{
    UserId = validation.UserId,
    Action = "SYSTEM_INFO_REQUEST",
    Success = true
};
```

---

## 🔄 **Auto-refresh de Token**

```csharp
// Dans NetworkClient ou un timer
if (authClient.IsAuthenticated)
{
    if (!authClient.IsTokenValid())
    {
        await authClient.RefreshTokenAsync();
    }
}
```

**Intervalle recommandé:** Vérifier toutes les 5 minutes.

---

**Version:** 1.0  
**Dernière mise à jour:** Février 2026
