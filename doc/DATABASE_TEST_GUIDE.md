# Guide de test - Configuration de la base de données

## Test 1: Vérifier la création de la base de données

### Commandes
```bash
cd C:\Users\HP\Desktop\NetAdminPro\NetAdmin.Server
dotnet build
```

### Résultat attendu
```
✅ Build réussi avec 4 avertissements (non-bloquants)
  NetAdmin.Shared net10.0 a réussi
  NetAdmin.Server net10.0-windows a réussi avec 2 avertissement(s)
```

### Vérification
```bash
# Vérifier l'existence du fichier de base de données
Get-ChildItem netadmin.db
Get-Item netadmin.db | Select-Object -Property Length, LastWriteTime
```

**Résultat attendu:**
```
Mode: -a---- (fichier normal)
Length: > 20000 bytes
LastWriteTime: Date/Heure actuelle
```

---

## Test 2: Initialisation de la base de données

### Commandes
```bash
cd C:\Users\HP\Desktop\NetAdminPro\NetAdmin.Server
dotnet build
# Attendre que le serveur crée la base de données
# Le serveur va bloquer sur l'interface WPF, appuyer sur Escape ou fermer la fenêtre
```

### Résultat attendu dans la console

```
[STARTUP] Initialisation du système...
[STARTUP] Configuration JWT chargée
[STARTUP] Contexte de base de données créé
[STARTUP] Services créés
[DB] Initialisation de la base de données...
[DB] Utilisateurs par défaut créés avec succès!
[DB] Identifiants disponibles:
     - admin / Admin@123!
     - supervisor / Supervisor@123!
     - operator / Operator@123!
     - viewer / Viewer@123!

=== ÉTAT DE LA BASE DE DONNÉES ===

[DB] Total d'utilisateurs: 4
  - admin (Administrator) - Email: admin@netadminpro.local - Actif: True
  - supervisor (Supervisor) - Email: supervisor@netadminpro.local - Actif: True
  - operator (Operator) - Email: operator@netadminpro.local - Actif: True
  - viewer (Viewer) - Email: viewer@netadminpro.local - Actif: True

[DB] Total de tokens: 4
  - Token UserId 1 - Actif: True - Expire: [Date future]
  - Token UserId 2 - Actif: True - Expire: [Date future]
  - Token UserId 3 - Actif: True - Expire: [Date future]
  - Token UserId 4 - Actif: True - Expire: [Date future]

[DB] Statistiques:
  - Logs d'audit: 0
  - Clients enregistrés: 0
  - Logs de metrics: 0

=== FIN DU RAPPORT ===

[STARTUP] Démarrage de l'interface utilisateur...
```

### Vérification
1. Quatre utilisateurs doivent être affichés ✅
2. Chaque utilisateur doit avoir un token actif ✅
3. Les statistiques doivent être zéro pour les nouvelles installations ✅

---

## Test 3: Vérifier que l'initialisation est idempotente

### Commandes
```bash
# Relancer le serveur
dotnet run
# Attendre quelques secondes
# Fermer l'application
```

### Résultat attendu

La deuxième exécution doit afficher:
```
[DB] Base de données déjà initialisée.
```

**Important**: Les utilisateurs ne doivent PAS être dupliqués.

### Vérification
```bash
# Vérifier que la base de données n'a toujours que 4 utilisateurs
# et 4 tokens
```

---

## Test 4: Vérifier la base de données avec SQLite

### Installation de SQLite CLI (si nécessaire)
```bash
# Télécharger depuis https://www.sqlite.org/download.html
# OU utiliser un outil GUI comme DB Browser for SQLite
```

### Commandes SQLite
```sql
-- Afficher tous les utilisateurs
SELECT * FROM Users;

-- Afficher tous les tokens
SELECT * FROM AuthTokens;

-- Afficher les statistiques
SELECT COUNT(*) as UserCount FROM Users;
SELECT COUNT(*) as TokenCount FROM AuthTokens;

-- Vérifier un utilisateur spécifique
SELECT * FROM Users WHERE Username = 'admin';

-- Vérifier les mots de passe hachés
SELECT Username, PasswordHash FROM Users;
```

### Résultat attendu

**Utilisateurs (4 lignes):**
```
Id | Username   | PasswordHash (BCrypt) | Email                   | FullName              | Role | IsActive | CreatedAt
1  | admin      | $2a$10$...           | admin@netadminpro.local | Administrateur Sys... | 0    | 1        | 2026-02-03...
2  | supervisor | $2a$10$...           | supervisor@netadmin...  | Superviseur           | 1    | 1        | 2026-02-03...
3  | operator   | $2a$10$...           | operator@netadmin...    | Opérateur             | 2    | 1        | 2026-02-03...
4  | viewer     | $2a$10$...           | viewer@netadmin...      | Lecteur               | 3    | 1        | 2026-02-03...
```

**Tokens (4 lignes):**
```
Id | UserId | Token (JWT...)      | RefreshToken (...)  | IssuedAt      | ExpiresAt
1  | 1      | eyJhbGc...          | [RefreshToken...]   | 2026-02-03... | 2026-02-04...
2  | 2      | eyJhbGc...          | [RefreshToken...]   | 2026-02-03... | 2026-02-04...
3  | 3      | eyJhbGc...          | [RefreshToken...]   | 2026-02-03... | 2026-02-04...
4  | 4      | eyJhbGc...          | [RefreshToken...]   | 2026-02-03... | 2026-02-04...
```

---

## Test 5: Vérifier la validité des tokens JWT

### Décoder un JWT (utiliser jwt.io ou un outil CLI)

1. Copier le token JWT d'un utilisateur de la base de données
2. Accéder à https://jwt.io
3. Coller le token dans le champ "Encoded"

**Payload attendu (exemple):**
```json
{
  "sub": "1",
  "username": "admin",
  "email": "admin@netadminpro.local",
  "role": "Administrator",
  "iat": 1675361000,
  "exp": 1675364600
}
```

**Vérification:**
- ✅ `sub` = Id de l'utilisateur
- ✅ `username` = Identifiant exact
- ✅ `role` = Rôle correct
- ✅ `exp` = Dans le futur (token valide)

---

## Test 6: Vérifier les mots de passe hachés (BCrypt)

### Utiliser un validateur BCrypt en ligne
1. Accéder à https://bcrypt-generator.com/ ou similaire
2. Entrer un mot de passe test : `Admin@123!`
3. Entrer le hash BCrypt de la base de données (admin)
4. Vérifier

**Résultat attendu:**
```
✅ Password matches hash
```

---

## Test 7: Vérifier la suppression et recréation

### Commandes
```bash
# Supprimer la base de données
Remove-Item netadmin.db

# Relancer le serveur
dotnet run
```

### Résultat attendu

```
[DB] Initialisation de la base de données...
[DB] Utilisateurs par défaut créés avec succès!
```

**Vérification:**
- La base de données est recréée ✅
- Les 4 utilisateurs par défaut sont créés ✅
- Les anciens tokens ne sont plus présents ✅

---

## Test 8: Vérifier la contrainte d'unicité sur le username

### Commandes SQL
```sql
-- Tenter d'insérer un utilisateur avec un username existant
INSERT INTO Users (Username, PasswordHash, Email, FullName, Role, IsActive, CreatedAt)
VALUES ('admin', '$2a$10$...', 'newemail@test.com', 'Test', 0, 1, datetime('now'));
```

### Résultat attendu
```
❌ UNIQUE constraint failed: Users.Username
```

---

## Test 9: Vérifier la contrainte d'unicité sur l'email

### Commandes SQL
```sql
-- Tenter d'insérer un utilisateur avec un email existant
INSERT INTO Users (Username, PasswordHash, Email, FullName, Role, IsActive, CreatedAt)
VALUES ('testuser', '$2a$10$...', 'admin@netadminpro.local', 'Test', 0, 1, datetime('now'));
```

### Résultat attendu
```
❌ UNIQUE constraint failed: Users.Email
```

---

## Checklist de validation

- [ ] Compilation sans erreurs
- [ ] Base de données créée (netadmin.db)
- [ ] 4 utilisateurs créés
- [ ] 4 tokens générés
- [ ] Tokens JWT valides
- [ ] Mots de passe hachés avec BCrypt
- [ ] Initialisation idempotente (pas de doublons)
- [ ] Contraintes d'unicité fonctionnelles
- [ ] Suppression et recréation possible
- [ ] Messages de console lisibles et corrects

## Dépannage

### Erreur: "database is locked"
```bash
# Arrêter tous les processus dotnet
taskkill /IM dotnet.exe /F

# Supprimer et recommencer
Remove-Item netadmin.db
dotnet build
```

### Erreur: "Le propriété 'X' non-nullable doit contenir une valeur"
- Vérifier que toutes les propriétés string sont initialisées avec `= string.Empty;`
- Vérifier que les navigation properties sont initialisées avec `= null!;`

### Les utilisateurs ne sont pas créés
- Vérifier les permissions d'accès au répertoire
- Vérifier l'espace disque disponible
- Vérifier les logs de console pour les erreurs d'authentification

### JWT Secret non valide
- Vérifier que le secret a au minimum 32 caractères
- Vérifier qu'aucun caractère spécial ne pose problème
