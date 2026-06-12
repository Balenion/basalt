# AuthForge 🛡️

**AuthForge** is a specialized tool for developers and security professionals designed for generating reliable cryptographic resources, analyzing tokens, and testing authentication mechanisms. The application focuses on creating tamper-resistant passwords, secret keys, and cryptographic data packages, operating entirely locally.

### Features

- **Key Generation**: Creating high-entropy secret keys of a specified bit depth (128, 256, 512 bits) for JWT (JSON Web Tokens), APIs, and other cryptographic needs.  
- **Password Engineering (Single Hash)**: Hashing single passwords with customizable complexity and salting for maximum brute-force resistance. Modern security standards are fully supported.
- **Batch Processing & Excel Integration**: 
	- **Import**: Loading account or password lists from Excel files. A built-in template generator is available for correct formatting.
	- **Processing**: Bulk parallel hashing of credentials using the selected algorithm (Argon2id, BCrypt, etc.) with automatic generation of a unique salt for each row.
	- **Export**: Exporting results to XLSX format using the ClosedXML library for direct integration into databases or migration systems.
- **Database Seed Generator**: A module for generating initial mock user data fully in English (Full Names, usernames, international phone numbers, roles, and statuses). Includes a deterministic distribution calculation for 2FA based on a user-specified percentage without "idle" random cycles on small samples.
- **2FA Codes (TOTP)**: A built-in tool for generating and verifying two-factor authentication time-based one-time passwords based on Base32 secret keys using the `Otp.NET` library. Equipped with a visual 30-second token lifetime progress bar and a quick copy function.
- **JWT Decoder / Inspector**: A lightweight tool for real-time parsing and validation of JSON Web Tokens without heavy external dependencies.
	- **Parsing**: Decoding the token structure into Header and Payload with clean JSON formatting (Pretty Print).
	- **Signature Verification**: Verifying cryptographic signatures for HMAC algorithms (HS256, HS384, HS512) using secure fixed-time comparison.
	- **Expiration Control**: Automatic parsing of token expiration (`exp`) converting the Unix timestamp to local time with a visual activity status indicator.
- **Zero Knowledge**: The application operates entirely offline. No input data, generated passwords, tokens, or keys are stored or transmitted over the network.

### Tech Stack  

- **Language**: C#  
- **Platform**: .NET 10 / WPF  
- **UI Design**: Material Design in XAML
- **Cryptography & Data (NuGet)**:
	- Konscious.Security.Cryptography.Argon2
	- BCrypt.Net-Next
	- Otp.NET
	- ClosedXML
	- System.Security.Cryptography
- **Supported Algorithms**:
	- Argon2id (customizable memory, iterations, and threads)
	- BCrypt (customizable Work Factor)
	- PBKDF2 (selectable SHA256 / SHA512 PRF and iteration count)
	- SHA256 (Legacy)
	- HMAC-SHA256 / HMAC-SHA384 / HMAC-SHA512 (within the JWT module)

### License

This project is licensed under the MIT License.