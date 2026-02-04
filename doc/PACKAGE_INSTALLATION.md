# 📦 Installation des Dépendances

## Packages Requis pour l'Authentification

Deux packages essentiels doivent être ajoutés au projet `NetAdmin.Server`:

### 1. **BCrypt.Net-Next**
- **Utilité:** Hash sécurisé des mots de passe
- **Fournisseur:** Erik Brakkee & Contributors
- **Package:** `BCrypt.Net-Next`

### 2. **JWT (System.IdentityModel.Tokens.Jwt)**
- **Utilité:** Support des tokens JWT
- **Fournisseur:** Microsoft
- **Packages:** 
  - `System.IdentityModel.Tokens.Jwt`
  - `Microsoft.IdentityModel.Tokens`

---

## 🔧 Installation (Choix la Méthode)

### Méthode 1: Commande dotnet (Recommandé)

Ouvrir une console PowerShell/CMD dans le dossier du projet:

```powershell
# Naviguer au dossier du serveur
cd "C:\Users\HP\Desktop\NetAdminPro\NetAdmin.Server"

# Ajouter BCrypt
dotnet add package BCrypt.Net-Next

# Ajouter JWT
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.IdentityModel.Tokens
```

### Méthode 2: NuGet Package Manager (Visual Studio)

1. Clic-droit sur **NetAdmin.Server** → **Manage NuGet Packages**
2. Aller à l'onglet **Browse**
3. Chercher et installer:
   - `BCrypt.Net-Next` (latest stable)
   - `System.IdentityModel.Tokens.Jwt` (latest stable)
   - `Microsoft.IdentityModel.Tokens` (latest stable)

### Méthode 3: Édition directe du .csproj

Ouvrir `NetAdmin.Server.csproj` et ajouter dans `<ItemGroup>`:

```xml
<ItemGroup>
  <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.0" />
  <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="7.0.0" />
</ItemGroup>
```

Puis:
```bash
dotnet restore
```

---

## ✅ Vérification de l'Installation

Après installation, vérifier avec:

```bash
cd NetAdmin.Server
dotnet build
```

**Résultat attendu:**
```
Build started...
Build succeeded.
```

**Si erreur:**
```
error NU1102: Unable to find package ...
```
→ Relancer: `dotnet nuget update source`

---

## 📋 Versions Recommandées (au 4 février 2026)

| Package | Version | .NET Min |
|---------|---------|----------|
| BCrypt.Net-Next | 4.0.3+ | .NET 4.6+ |
| System.IdentityModel.Tokens.Jwt | 7.0.0+ | .NET 4.7.2+ |
| Microsoft.IdentityModel.Tokens | 7.0.0+ | .NET 4.7.2+ |

---

## 🔍 Diagnostique

### Erreur: "NuGet not found"
```powershell
# Restaurer NuGet
dotnet nuget update source
```

### Erreur: "Package version conflict"
```powershell
# Forcer la réinstallation
dotnet package remove BCrypt.Net-Next
dotnet add package BCrypt.Net-Next
```

### Erreur: "NET 10.0 not compatible"
Utiliser les versions stables (v6 ou v7) qui sont compatibles avec .NET 10.0

---

## 🚀 Après Installation

1. ✅ Ajouter les `using` statements aux fichiers concernés:

```csharp
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
```

2. ✅ Compiler le projet:
```bash
dotnet build
```

3. ✅ Tester le service:
```bash
dotnet run
```

---

## 📚 Documentationdes Packages

- [BCrypt.Net Documentation](https://github.com/BcryptNet/bcrypt.net)
- [JWT Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt)
- [IdentityModel Tokens](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens)

---

**Créé:** Février 2026  
**Statut:** À faire avant de compiler
