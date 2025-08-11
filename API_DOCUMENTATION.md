# Proje Takip Sistemi - Mobil API Dokümantasyonu

## Genel Bilgiler

Bu API, Proje Takip Sistemi'nin mobil uygulamalar için geliştirilmiş RESTful API'sidir.

**Base URL:** `http://localhost:5264/api`
**API Dokümantasyonu:** `http://localhost:5264/api-docs` (Swagger UI)

## Authentication

API, Bearer token tabanlı authentication kullanır.

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "kimlik": "admin",
  "sifre": "123456"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Giriş başarılı",
  "data": {
    "sessionToken": "base64-encoded-token",
    "expiryTime": "2024-01-01T12:00:00Z",
    "user": {
      "id": 1,
      "adSoyad": "Admin Kullanıcı",
      "kimlik": "admin",
      "rol": 1
    }
  }
}
```

### Token Kullanımı
Tüm API isteklerinde Authorization header'ı kullanın:
```http
Authorization: Bearer {sessionToken}
```

## API Endpoints

### 1. Authentication (`/api/auth`)

- `POST /login` - Kullanıcı girişi
- `POST /logout` - Kullanıcı çıkışı
- `GET /validate-token` - Token geçerliliği kontrolü

### 2. Projeler (`/api/projects`)

- `GET /` - Tüm projeleri listele
  - Query parametreleri: `durum`, `mudurluk`
- `GET /{id}` - Belirli bir projeyi getir
- `POST /` - Yeni proje oluştur
- `PUT /{id}` - Projeyi güncelle
- `DELETE /{id}` - Projeyi sil
- `GET /statistics` - Proje istatistikleri

### 3. İlerleme (`/api/progress`)

- `GET /project/{projeId}` - Proje ilerlemelerini getir
- `POST /add-progress` - Yeni ilerleme ekle
- `PUT /update-progress/{id}` - İlerlemeyi güncelle
- `DELETE /delete-progress/{id}` - İlerlemeyi sil
- `POST /add-gantt-stage` - Gantt aşaması ekle

### 4. Kullanıcılar (`/api/users`)

- `GET /` - Tüm kullanıcıları listele
  - Query parametresi: `rol`
- `GET /{id}` - Belirli bir kullanıcıyı getir
- `POST /` - Yeni kullanıcı oluştur
- `PUT /{id}` - Kullanıcıyı güncelle
- `DELETE /{id}` - Kullanıcıyı sil
- `GET /statistics` - Kullanıcı istatistikleri
- `POST /change-password` - Şifre değiştir

### 5. Sistem Logları (`/api/systemlogs`)

- `GET /` - Sistem loglarını listele
  - Query parametreleri: `page`, `pageSize`, `logType`, `executor`, `startDate`, `endDate`
- `GET /{id}` - Belirli bir log kaydını getir
- `GET /statistics` - Log istatistikleri
- `GET /recent` - Son aktiviteler
- `GET /user-activities/{userId}` - Kullanıcı aktiviteleri
- `DELETE /clear-old-logs` - Eski logları temizle

### 6. Birimler (`/api/units`)

- `GET /` - Tüm birimleri listele
- `GET /{id}` - Belirli bir birimi getir
- `POST /` - Yeni birim oluştur
- `PUT /{id}` - Birimi güncelle
- `DELETE /{id}` - Birimi sil
- `GET /statistics` - Birim istatistikleri

### 7. Sponsorlar (`/api/sponsors`)

- `GET /` - Tüm sponsorları listele
- `GET /{id}` - Belirli bir sponsoru getir
- `POST /` - Yeni sponsor oluştur
- `PUT /{id}` - Sponsoru güncelle
- `DELETE /{id}` - Sponsoru sil
- `GET /statistics` - Sponsor istatistikleri
- `GET /search` - Sponsor arama

### 8. Raporlar (`/api/reports`)

- `GET /GetProjeRaporu` - Proje raporu getir
  - Query parametresi: `projeId`

## Örnek Kullanım Senaryoları

### Mobil Uygulama Giriş Akışı

1. **Login:**
```javascript
const loginResponse = await fetch('http://localhost:5264/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    kimlik: 'admin',
    sifre: '123456'
  })
});

const loginData = await loginResponse.json();
const token = loginData.data.sessionToken;
```

2. **Projeleri Listele:**
```javascript
const projectsResponse = await fetch('http://localhost:5264/api/projects', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

const projects = await projectsResponse.json();
```

3. **İlerleme Ekle:**
```javascript
const progressResponse = await fetch('http://localhost:5264/api/progress/add-progress', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    projeID: 1,
    ganttID: 1,
    ilerlemeTanimi: 'Yeni ilerleme',
    tamamlanmaYuzdesi: 75,
    aciklama: 'İlerleme açıklaması',
    kullaniciID: 1
  })
});
```

## Hata Kodları

- `200` - Başarılı
- `400` - Geçersiz istek
- `401` - Yetkisiz erişim
- `404` - Bulunamadı
- `500` - Sunucu hatası

## Response Formatı

Tüm API yanıtları aşağıdaki formatta döner:

```json
{
  "success": true/false,
  "message": "İşlem mesajı",
  "data": {}, // Veri objesi (opsiyonel)
  "count": 0, // Kayıt sayısı (listeler için)
  "pagination": {} // Sayfalama bilgisi (opsiyonel)
}
```

## Test Kullanıcıları

- **Admin:** `kimlik: admin`, `sifre: 123456`, `rol: 1`
- **Test:** `kimlik: test`, `sifre: 123456`, `rol: 2`
- **Dev:** `kimlik: dev`, `sifre: 123456`, `rol: 3`

## Notlar

- API, CORS ayarları ile tüm origin'lere açıktır (development için)
- Session token'lar 24 saat geçerlidir
- Tüm tarih/saat değerleri UTC formatındadır
- Sayfalama varsayılan olarak sayfa başına 50 kayıt gösterir
- Log kayıtları otomatik olarak oluşturulur ve IP adresi ile birlikte saklanır

## Güvenlik

- Production ortamında HTTPS kullanılmalıdır
- CORS ayarları production için kısıtlanmalıdır
- Şifreler hash'lenmelidir (şu anda plain text)
- JWT token implementasyonu önerilir
- Rate limiting eklenmelidir