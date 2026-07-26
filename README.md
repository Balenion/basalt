# Basalt 🛡️

**Basalt** is a specialized, production-ready desktop tool for developers and security professionals designed for generating reliable cryptographic resources, analyzing secure tokens, and testing authentication mechanisms. Operating completely offline under a strict zero-knowledge architecture, it focuses on providing an intuitive yet powerful interface for complex cryptographic operations.

---

## Technical Interface & Features

### 🔑 Key Generation
Generates high-entropy cryptographically secure pseudo-random secrets with targeted bit depths (128, 256, or 512 bits). Outputs are automatically encoded into multiple standard formats concurrently alongside context-aware integration guides.

<details>
  <summary>📸 View Key Generation Module Interface</summary>

  ![Key Generation Module](assets/keys_tab.png)

</details>

* **Multi-Format Encoding:** Provides instantaneous output for Base64 strings, raw HEX streams, and stripped Base32 formats.
* **Adaptive Guidance:** Displays direct documentation mappings detailing precise use-cases (e.g., JWT Signing Keys, AES symmetric keys, or TOTP registration strings) depending on the selected length.

---

### 🧮 Password Engineering (Single Hash)
Allows real-time evaluation and configuration of single-credential hashing models leveraging underlying parameters pulled directly from the batch-processing engine.

<details>
  <summary>📸 View Single Hash Module Interface</summary>

  ![Single Hash Module](assets/single_tab.png)

</details>

* **State Inheritance:** Inherits runtime parameters seamlessly from the primary Excel configuration view to prevent administrative workflow disconnects.
* **Isolated Output Inspectors:** Separates the finalized password string hash from its unique Base64 cryptographically secure initialization vector (Salt) for deep inspecting.

---

### 📊 Batch Processing & Excel Integration
Designed for enterprise migration, database population, and bulk account audits. Supports flexible, multi-threaded calculations across four core algorithmic strategies.

<details>
  <summary>⚙️ View Argon2id Parameter Configuration</summary>

  ![Argon2id Hashing Config](assets/excel_tab_argon2.png)

</details>

<details>
  <summary>⚙️ View BCrypt Parameter Configuration</summary>

  ![BCrypt Hashing Config](assets/excel_tab_bcrypt.png)

</details>

<details>
  <summary>⚙️ View PBKDF2 Parameter Configuration</summary>

  ![PBKDF2 Hashing Config](assets/excel_tab_pbkdf2.png)

</details>

<details>
  <summary>⚙️ View Legacy SHA256 Configuration & Warnings</summary>

  ![Legacy Hashing Config](assets/excel_tab_legacy.png)

</details>

* **Template Automation:** Features an integrated spreadsheet schema generator (`ClosedXML`) creating clean, structured target fields ensuring data parsing compatibility.
* **Granular Hardware Sliders:** Direct tuning bounds over intense memory footprints (RAM allocation limits), compute iteration passes, and parallel thread limits matching specialized hardware environments.
* **Cryptographic Safeguards:** Includes contextual alerts and proactive mitigation systems (e.g., automatic warning banners when attempting un-salted legacy SHA256 computations).

---

### 🌱 Database Seed Generator
Generates mock user environments to facilitate deterministic database seeding without the processing inefficiencies or data overlap characteristic of simple randomized loops.

<details>
  <summary>📸 View Database Seed Generator Interface</summary>

  ![Database Seed Generator](assets/seed_gen_tab.png)

</details>

* **Entity Customization:** Supports optional targeted parameters including full names, structural breakdown splits (Last/First/Middle formatting blocks), randomized matching passwords, emails, secure telephone links, security access levels, and active system operational states.
* **Proportional 2FA Secret Seeding:** Employs a zero-waste target distribution engine assigning Base32 secrets across exact user-specified sample percentages, optimizing database population on smaller unit testing pools.

---

### ⏱️ 2FA Codes (TOTP Monitor)
A diagnostic utility verifying the validity, time-drift synchronization, and parsing accurate token generation for time-based one-time authorization tokens.

<details>
  <summary>📸 View 2FA Monitor Interface</summary>

  ![2FA TOTP Monitor](assets/2fa_codes_tab.png)

</details>

* **Active Lifetime Tracker:** Features a precision 30-second countdown system tied to a clean UI progress visual overlay reflecting transient authentication valid ranges.
* **Robust Key Normalization:** Cleans formatting variances during manual key entry to seamlessly parse raw authorization structures.

---

### 🔍 JWT Decoder / Inspector
A diagnostic view built for real-time decoding, structural dissection, and signature confirmation of JSON Web Tokens without relying on online validation tools.

<details>
  <summary>📸 View JWT Decoder Interface</summary>

  ![JWT Decoder](assets/jwt_decoder_tab.png)

</details>

* **Formatted Syntax Highlighting:** Decodes composite base64 strings into readable structured formats parsing structural components (Header information vs Claim payloads).
* **Fixed-Time Verification:** Protects verification mechanisms from side-channel timing analysis vectors during HMAC validation sweeps (`HS256`, `HS384`, `HS512`).
* **Temporal Normalization:** Instantly translates implicit Unix timestamps (`exp` claims) into system local time values while rendering persistent operational indicators.

---

## Tech Stack & Architecture

- **Language:** C#
- **Platform:** .NET 10 / Windows Presentation Foundation (WPF)
- **UI Design:** Material Design in XAML (v5.0+ Base Theme Layer)

### Core Dependencies (NuGet)
* `MaterialDesignThemes` — Modern component rendering & styling suite
* `Konscious.Security.Cryptography.Argon2` — Argon2id implementation
* `BCrypt.Net-Next` — Enhanced BCrypt core wrapper
* `Otp.NET` — Standard-compliant RFC TOTP calculation engine
* `ClosedXML` — Strict open XML spreadsheet creation logic
* `System.Security.Cryptography` — Native cryptographic abstractions

---

## 🧾 License

This project is licensed under the MIT License.
