# 🏪 Inventory Management REST API

> **ASP.NET Core 8 · Entity Framework Core · JWT Auth · Swagger**

---

## 📋 Përmbledhje e Projektit

REST API e plotë për menaxhimin e inventarit të një dyqani online, ndërtuar me teknologjitë më të reja .NET.

---

## 🏗️ Arkitektura

```
InventoryAPI/
├── Controllers/
│   ├── AuthController.cs          # Login, Register, /me
│   ├── ProductsController.cs      # CRUD + Stock Adjustment
│   ├── CategoriesController.cs    # CRUD kategorish
│   └── AuditController.cs         # Log i ndryshimeve
├── Models/
│   ├── User.cs                    # Entiteti i përdoruesit
│   ├── Product.cs                 # Entiteti i produktit
│   ├── Category.cs                # Entiteti i kategorisë
│   └── AuditLog.cs               # Log i auditimit
├── DTOs/
│   ├── ProductDTOs.cs            # Create/Update/Response/Filter DTOs
│   ├── AuthDTOs.cs               # Login/Register/Response DTOs
│   └── CategoryDTOs.cs           # Category DTOs
├── Services/
│   ├── ProductService.cs          # Logjika e biznesit për produkte
│   ├── CategoryService.cs         # Logjika e biznesit për kategori
│   └── AuthService.cs            # JWT generation, BCrypt
├── Data/
│   └── AppDbContext.cs            # EF Core DbContext + Seed Data
├── Middleware/
│   └── ExceptionMiddleware.cs     # Global error handling
├── appsettings.json               # Konfigurimi (DB, JWT)
└── Program.cs                     # DI Container, Middleware Pipeline
```

---

## 🗄️ Modeli i të Dhënave

```
┌─────────────┐       ┌──────────────┐       ┌───────────────┐
│   Category  │ 1───n │   Product    │ 1───n │   AuditLog    │
├─────────────┤       ├──────────────┤       ├───────────────┤
│ Id          │       │ Id           │       │ Id            │
│ Name        │       │ Name         │       │ ProductId(FK) │
│ Description │       │ SKU (unique) │       │ Action        │
│ ImageUrl    │       │ Description  │       │ ChangedBy     │
│ IsActive    │       │ Price        │       │ OldValues     │
│ CreatedAt   │       │ CostPrice    │       │ NewValues     │
└─────────────┘       │ StockQty     │       │ QtyBefore     │
                      │ LowStockThrs │       │ QtyAfter      │
┌─────────────┐       │ Barcode      │       │ Notes         │
│    User     │       │ ImageUrl     │       │ Timestamp     │
├─────────────┤       │ IsActive     │       └───────────────┘
│ Id          │       │ CategoryId   │
│ Username    │       │ CreatedAt    │
│ Email       │       │ UpdatedAt    │
│ PasswordHash│       └──────────────┘
│ Role        │
│ IsActive    │
└─────────────┘
```

---

## 🔐 Sistemi i Autentikimit

| Role    | Të drejtat                                              |
|---------|--------------------------------------------------------|
| Admin   | Gjithçka + Delete + Register përdorues + Statistika    |
| Manager | Create, Read, Update produkte/kategori + Stock Adjust  |
| Viewer  | Vetëm Read (GET endpoints)                             |

**Kredencialet e paracaktuar:**
- `admin / Admin@123` (Role: Admin)
- `manager / Manager@123` (Role: Manager)

---

## 🌐 API Endpoints

### 🔐 Auth
| Method | Endpoint           | Auth     | Përshkrimi              |
|--------|--------------------|----------|-------------------------|
| POST   | /api/auth/login    | ❌ Public| Kyçja + JWT Token       |
| POST   | /api/auth/register | 🔴 Admin | Regjistro përdorues     |
| GET    | /api/auth/me       | ✅ Any   | Info i userит aktual    |

### 📦 Products
| Method | Endpoint                      | Auth         | Përshkrimi                  |
|--------|-------------------------------|--------------|------------------------------|
| GET    | /api/products                 | ❌ Public    | Listo me paginim + filtra    |
| GET    | /api/products/{id}            | ❌ Public    | Merr produkt sipas ID        |
| GET    | /api/products/sku/{sku}       | ❌ Public    | Merr produkt sipas SKU       |
| POST   | /api/products                 | 🟡 Manager  | Krijo produkt                |
| PUT    | /api/products/{id}            | 🟡 Manager  | Përditëso produkt            |
| DELETE | /api/products/{id}            | 🔴 Admin    | Soft delete (çaktivizo)      |
| PATCH  | /api/products/{id}/stock      | 🟡 Manager  | Rregulllo stokun             |
| GET    | /api/products/alerts/low-stock| ✅ Auth     | Produkte me stok të ulët     |
| GET    | /api/products/alerts/out-of-stock| ✅ Auth  | Produkte pa stok             |

### 📂 Categories
| Method | Endpoint              | Auth         | Përshkrimi         |
|--------|-----------------------|--------------|--------------------|
| GET    | /api/categories       | ❌ Public    | Listo kategoritë   |
| GET    | /api/categories/{id}  | ❌ Public    | Merr kategorinë    |
| POST   | /api/categories       | 🟡 Manager  | Krijo kategori     |
| PUT    | /api/categories/{id}  | 🟡 Manager  | Përditëso          |
| DELETE | /api/categories/{id}  | 🔴 Admin    | Soft delete        |

### 📋 Audit
| Method | Endpoint                       | Auth        | Përshkrimi                |
|--------|--------------------------------|-------------|---------------------------|
| GET    | /api/audit                     | 🟡 Manager | Të gjitha loglat          |
| GET    | /api/audit/product/{productId} | ✅ Auth    | Historia e produktit      |
| GET    | /api/audit/statistics          | 🔴 Admin   | Statistika të aktivitetit |

---

## 🔍 Filtrimi i Avancuar (GET /api/products)

```
GET /api/products?search=laptop&categoryId=1&isLowStock=true&minPrice=100&maxPrice=500&sortBy=price&sortOrder=desc&page=1&pageSize=10
```

| Parametri    | Lloji   | Përshkrimi                              |
|--------------|---------|-----------------------------------------|
| search       | string  | Kërkon në emër, SKU, përshkrim, barcode |
| categoryId   | int     | Filtro sipas kategorisë                 |
| isActive     | bool    | Filtro sipas statusit                   |
| isLowStock   | bool    | Vetëm me stok të ulët                   |
| isOutOfStock | bool    | Vetëm pa stok                           |
| minPrice     | decimal | Çmim minimal                            |
| maxPrice     | decimal | Çmim maksimal                           |
| sortBy       | string  | name, price, stock, createdAt           |
| sortOrder    | string  | asc, desc                               |
| page         | int     | Numri i faqes (default: 1)              |
| pageSize     | int     | Artikuj për faqe (default: 20)          |

---

## 🧪 Si të Testoni

### 1. Startoni aplikacionin
```bash
cd InventoryAPI
dotnet run
```

### 2. Hapni Swagger UI
```
http://localhost:5000
```

### 3. Kyçuni dhe merrni token
```json
POST /api/auth/login
{
  "username": "admin",
  "password": "Admin@123"
}
```

### 4. Autorizohuni në Swagger
Klikoni butonin **Authorize** → `Bearer {token_juaj}`

---

## 📊 Krahasimi me Shopify API (Vlerësim)

| Funksionalitet         | InventoryAPI    | Shopify API      | Vlerësimi     |
|------------------------|-----------------|------------------|---------------|
| CRUD Produkte          | ✅ Plotë        | ✅ Plotë          | Barabar       |
| Stock Tracking         | ✅ Manual       | ✅ Multi-location | Shopify+      |
| Kërkim & Filtra        | ✅ 8+ parametra | ✅ GraphQL        | Barabar       |
| Audit Log              | ✅ I plotë      | ⚠️ Partial        | **Ours+**     |
| JWT Auth               | ✅ Native       | ✅ OAuth 2.0      | Shopify+      |
| Role-based Access      | ✅ 3 Role       | ✅ Staff roles    | Barabar       |
| Swagger/Docs           | ✅ Built-in     | ✅ Shopify docs   | Barabar       |
| Webhooks               | ❌ N/A          | ✅ Plotë          | Shopify+      |
| Multi-currency         | ❌ N/A          | ✅ Built-in       | Shopify+      |
| Rate Limiting          | ❌ N/A          | ✅ 40 req/s       | Shopify+      |

**Konkluzion:** Për menaxhimin e inventarit bazë, ky API ofron funksionalitet të plotë dhe auditim superior ndaj Shopify. Shopify mbijeton për platforma të mëdha me shumë lokacione dhe webhook integrime.

---

## ⚙️ Teknologjitë

| Teknologjia                      | Versioni | Roli                           |
|----------------------------------|----------|--------------------------------|
| ASP.NET Core                     | 8.0      | Web Framework                  |
| Entity Framework Core            | 8.0      | ORM (SQLite/SQL Server)        |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0 | JWT Auth         |
| Swashbuckle.AspNetCore           | 6.5      | Swagger UI & OpenAPI 3.0       |
| BCrypt.Net-Next                  | 4.0      | Password Hashing               |
| SQLite                           | -        | Databaza (dev/test)            |

