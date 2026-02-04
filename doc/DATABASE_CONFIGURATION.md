# Configuration de la Base de Données

## Vue d'ensemble

La configuration de la base de données NetAdminPro utilise **SQLite** avec **Entity Framework Core** pour une persistance de données simple et efficace. L'initialisation automatique crée le schéma de base de données et les utilisateurs par défaut au démarrage.

## Architecture de la base de données

### Fichier de base de données
- **Localisation**: `netadmin.db` (créé automatiquement à la racine du répertoire d'exécution)
- **Type**: SQLite 3
- **Chaîne de connexion**: `Data Source=netadmin.db`

### Schéma de la base de données

#### Table Users
Stocke les informations d'authentification des utilisateurs.

```
┌─────────────────────────────────┐
│         Users Table              │
├─────────────────────────────────┤
│ Id (Primary Key, Int)            │
│ Username (Unique, String)        │
│ PasswordHash (String - BCrypt)   │
│ Email (Unique, String)           │
│ FullName (String)                │
│ Role (Enum: Admin/Supervisor)    │
│ IsActive (Boolean)               │
│ CreatedAt (DateTime)             │
│ LastLoginAt (DateTime, Nullable) │
└─────────────────────────────────┘
```

#### Table AuthTokens
Stocke les jetons JWT et tokens de rafraîchissement.

```
┌──────────────────────────────┐
│   AuthTokens Table           │
├──────────────────────────────┤
│ Id (Primary Key, Int)         │
│ UserId (Foreign Key)          │
│ Token (String - JWT)          │
│ RefreshToken (String)         │
│ IssuedAt (DateTime)           │
│ ExpiresAt (DateTime)          │
│ RevokedAt (DateTime, Null)    │
│ IpAddress (String)            │
│ UserAgent (String)            │
└──────────────────────────────┘
```

#### Table AuditLogs
Enregistre les actions effectuées par les utilisateurs.

```
┌──────────────────────────────┐
│   AuditLogs Table            │
├──────────────────────────────┤
│ Id (Primary Key, Int)         │
│ UserId (Foreign Key, Null)    │
│ Timestamp (DateTime)          │
│ Action (String)               │
│ TargetMachine (String)        │
│ Username (String)             │
│ Details (String)              │
│ Success (Boolean)             │
└──────────────────────────────┘
```

#### Table ClientHosts
Enregistre les machines clients connectées.

```
┌──────────────────────────────┐
│   ClientHosts Table          │
├──────────────────────────────┤
│ Id (Primary Key, Int)         │
│ MachineName (String, Unique)  │
│ IpAddress (String)            │
│ OSVersion (String)            │
│ IsOnline (Boolean)            │
│ LastSeen (DateTime)           │
└──────────────────────────────┘
```

#### Table MetricLogs
Enregistre les métriques de performance.

```
┌──────────────────────────────┐
│   MetricLogs Table           │
├──────────────────────────────┤
│ Id (Primary Key, Long)        │
│ CpuUsage (Double)             │
│ RamAvailable (Double)         │
│ Timestamp (DateTime)          │
│ ClientHostId (Foreign Key)    │
└──────────────────────────────┘
```

## Initialisation automatique

### Processus d'initialisation

1. **Au démarrage du serveur**, `Program.cs` effectue les étapes suivantes:

```csharp
// 1. Crée le contexte AppDbContext
var context = new AppDbContext();

// 2. Crée le service d'authentification avec les paramètres JWT
var authService = new AuthenticationService(context, jwtSecret, 60, 7);

// 3. Crée l'initialisateur de base de données
var databaseInitializer = new DatabaseInitializer(context, authService);

// 4. Appelle Initialize()
databaseInitializer.Initialize();

// 5. Affiche le rapport de statut
DatabaseTest.DisplayDatabaseStatus();
```

### Étapes de l'initialisateur

`DatabaseInitializer.Initialize()` effectue les opérations suivantes:

1. **Création du schéma**: `_context.Database.EnsureCreated()`
   - Crée automatiquement toutes les tables et relations
   - N'effectue rien si les tables existent déjà

2. **Vérification de l'état**: `if (_context.Users.Any())`
   - Si des utilisateurs existent déjà, arrête le processus
   - Évite la duplication des utilisateurs par défaut

3. **Création des utilisateurs par défaut**:
   - **admin** / Admin@123! (Administrateur)
   - **supervisor** / Supervisor@123! (Superviseur)
   - **operator** / Operator@123! (Opérateur)
   - **viewer** / Viewer@123! (Lecteur)

4. **Affichage des résultats**: Liste les identifiants disponibles

## Utilisateurs par défaut

### Admin (Administrateur)
- **Identifiant**: admin
- **Mot de passe**: Admin@123!
- **Email**: admin@netadminpro.local
- **Rôle**: Administrator
- **Permissions**: Accès complet à toutes les fonctionnalités

### Supervisor (Superviseur)
- **Identifiant**: supervisor
- **Mot de passe**: Supervisor@123!
- **Email**: supervisor@netadminpro.local
- **Rôle**: Supervisor
- **Permissions**: Supervision et gestion

### Operator (Opérateur)
- **Identifiant**: operator
- **Mot de passe**: Operator@123!
- **Email**: operator@netadminpro.local
- **Rôle**: Operator
- **Permissions**: Opérations standard

### Viewer (Lecteur)
- **Identifiant**: viewer
- **Mot de passe**: Viewer@123!
- **Email**: viewer@netadminpro.local
- **Rôle**: Viewer
- **Permissions**: Lecture seule

## Configuration JWT

Les paramètres JWT sont définis dans `Program.cs`:

```csharp
string jwtSecret = "your-super-secret-key-min-32-characters-for-security";
int tokenExpirationMinutes = 60;           // Expiration du JWT
int refreshTokenExpirationDays = 7;        // Expiration du refresh token
```

### Paramètres JWT

| Paramètre | Valeur | Description |
|-----------|--------|-------------|
| Secret | 32+ caractères | Clé secrète pour signer les tokens (⚠️ À changer en production) |
| Token Expiration | 60 minutes | Durée de validité du JWT |
| Refresh Token Expiration | 7 jours | Durée de validité du refresh token |

## Affichage du statut de la base de données

Le serveur affiche automatiquement le rapport de statut après l'initialisation:

```
=== ÉTAT DE LA BASE DE DONNÉES ===

[DB] Total d'utilisateurs: 4
  - admin (Administrator) - Email: admin@netadminpro.local - Actif: True
  - supervisor (Supervisor) - Email: supervisor@netadminpro.local - Actif: True
  - operator (Operator) - Email: operator@netadminpro.local - Actif: True
  - viewer (Viewer) - Email: viewer@netadminpro.local - Actif: True

[DB] Total de tokens: 4
  - Token UserId 1 - Actif: True - Expire: 2026-02-04 21:15:00
  - Token UserId 2 - Actif: True - Expire: 2026-02-04 21:15:00
  - Token UserId 3 - Actif: True - Expire: 2026-02-04 21:15:00
  - Token UserId 4 - Actif: True - Expire: 2026-02-04 21:15:00

[DB] Statistiques:
  - Logs d'audit: 0
  - Clients enregistrés: 0
  - Logs de metrics: 0

=== FIN DU RAPPORT ===
```

## Réinitialisation de la base de données

Pour réinitialiser complètement la base de données:

```csharp
var context = new AppDbContext();
var authService = new AuthenticationService(context, jwtSecret, 60, 7);
var initializer = new DatabaseInitializer(context, authService);
initializer.Reset();  // Supprime et recrée tout
```

## Modifications de la base de données après initialisation

### Changer un mot de passe utilisateur

```csharp
var authService = new AuthenticationService(context, jwtSecret, 60, 7);
authService.ChangePassword(userId: 1, newPassword: "NewPassword@123");
```

### Désactiver un utilisateur

```csharp
var user = context.Users.First(u => u.Username == "viewer");
user.IsActive = false;
context.SaveChanges();
```

### Révoquer un token

```csharp
var token = context.AuthTokens.First(t => t.UserId == 1);
token.RevokedAt = DateTime.UtcNow;
context.SaveChanges();
```

## Gestion des migrations Entity Framework

Si vous modifiez le schéma, créez une migration:

```bash
# Ajouter une nouvelle migration
dotnet ef migrations add AddNewFeature

# Appliquer les migrations
dotnet ef database update
```

## Sauvegarde et restauration

### Sauvegarder la base de données

```bash
# Copier le fichier netadmin.db
copy netadmin.db netadmin.db.backup
```

### Restaurer la base de données

```bash
# Copier la sauvegarde
copy netadmin.db.backup netadmin.db
```

## Déploiement en production

### ⚠️ Modifications requises avant production

1. **Changer le JWT Secret**:
   ```csharp
   string jwtSecret = "generez-une-nouvelle-clé-secrète-très-longue-et-aléatoire";
   ```
   Minimum 32 caractères, préférablement 64+ caractères aléatoires.

2. **Changer les mots de passe par défaut**:
   ```csharp
   initializer.Reset();  // Recrée avec de nouveaux identifiants
   ```

3. **Activer HTTPS**:
   - Générer un certificat SSL
   - Configurer appsettings.json avec HTTPS

4. **Sauvegarder la base de données**:
   - Implémenter une stratégie de sauvegarde régulière

## Dépannage

### Le serveur se bloque lors de l'initialisation

Si le serveur semble figé, vérifiez:
- Les permissions sur le répertoire
- L'espace disque disponible
- La console pour les messages d'erreur

### Erreur "database is locked"

La base de données est verrouillée par un autre processus:
```bash
# Arrêter tous les processus dotnet
taskkill /IM dotnet.exe /F

# Supprimer et recréer
rm netadmin.db
dotnet run
```

### Les utilisateurs ne sont pas créés

1. Vérifier les logs de la console
2. Vérifier les permissions BCrypt
3. Réinitialiser avec `initializer.Reset()`

## Fichiers clés

- `Program.cs` - Point d'entrée et initialisation
- `AppDbContext.cs` - Contexte Entity Framework
- `DatabaseInitializer.cs` - Service d'initialisation
- `User.cs`, `AuthToken.cs`, `AuditLog.cs` - Entités
- `netadmin.db` - Fichier de base de données SQLite
