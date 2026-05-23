# Robocode TankRoyale - BSK GARED Team

---

## 📘 Panduan Penggunaan

Dokumentasi ini berisi:
- Konsep algoritma yang digunakan
- Strategi penyerangan & pertahanan
- Requirement project
- Cara instalasi
- Command build & running bot

---

## ⚡ Implementasi Algoritma Greedy

> [!NOTE]
> Algoritma greedy diterapkan untuk membantu bot mengambil keputusan tercepat dan paling optimal dalam situasi pertarungan.

### 🎯 Strategi Penyerangan

Bot dirancang untuk:
- Menyerang dari jarak jauh
- Meminimalkan risiko terkena serangan lawan
- Meningkatkan akurasi prediksi arah musuh
- Mengambil keputusan cepat saat duel berlangsung

Dengan pendekatan ini, bot memiliki peluang lebih besar untuk memperoleh point kemenangan secara konsisten.

---

### 🛡️ Strategi Pertahanan

Saat musuh mendekat:
- Bot akan bergerak mundur sambil memperhatikan batas arena
- Menghindari tabrakan dengan tembok
- Mencari jalur alternatif untuk tetap bergerak
- Menghindari posisi diam terlalu lama

Jika bot berada di posisi terpojok dan tidak memiliki ruang gerak:
- Bot akan masuk ke mode pertahanan terakhir
- Menyerang menggunakan power maksimal
- Fokus memberikan damage sebesar mungkin kepada lawan

---

## 📦 Requirement & Instalasi

> [!IMPORTANT]
> Bot berjalan menggunakan **.NET Runtime 8.0.27+**

### 🔧 Software Yang Wajib Diinstall

#### 1. Install .NET Runtime / SDK
```bash
https://dotnet.microsoft.com/download
```

#### 2. Install Robocode TankRoyale
```bash
https://robocode-dev.github.io/tank-royale/
```

---

## 🚀 Build & Running Bot

### 🧹 Clean Project
```bash
dotnet clean
```

### 🔄 Restore Dependency
> Jalankan jika terjadi error atau dependency belum terinstall.

```bash
dotnet restore
```

### ▶️ Menjalankan Bot
```bash
dotnet run
```

---

## 👨‍💻 Tim Pengembang

| Nama | NIM |
|---|---|
| Gede Valendra | 124140142 |
| Faisal H. Sinambela | 124140040 |
| Stevan Immanuel Simbolon | 124140130 |

---

## 🌐 Developer Profile

### Gede Valendra

- Instagram : https://instagram.com/gedevln12_
- GitHub : https://github.com/gedevalendra

---

<div align="center">

### Robocode Tank Royale X BSK GARED Team
Robocode TankRoyale Project

</div>