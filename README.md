<p align="center">
  <img src="https://raw.githubusercontent.com/slskd/slskd/master/docs/slskd.png" width="128" height="128" alt="slskdn logo">
</p>

<h1 align="center">slskdn</h1>

<p align="center">
  <strong>The batteries-included Soulseek web client</strong>
</p>

<p align="center">
  <a href="https://github.com/snapetech/slskdn/issues">Issues</a> •
  <a href="https://github.com/snapetech/slskdn/blob/master/FORK_VISION.md">Roadmap</a> •
  <a href="#features">Features</a> •
  <a href="#quick-start">Quick Start</a>
</p>

---

## What is slskdn?

**slskdn** is a feature-rich fork of [slskd](https://github.com/slskd/slskd), the modern web-based Soulseek client.

While slskd focuses on being a lean, API-first daemon that lets users implement advanced features via external scripts, **slskdn takes the opposite approach**:

> **Everything built-in. No scripts required.**

If you've ever seen a feature request closed with *"this can be done via the API with a script"* and thought *"but I just want it to work"*—slskdn is for you.

---

## Why Fork?

| slskd Philosophy | slskdn Philosophy |
|-----------------|-------------------|
| Lean core, script the rest | Batteries included |
| API-first, UI second | Rich UI experience |
| External integrations | Built-in features |
| Power users write scripts | Power users get features |

We're not replacing slskd—we're building on top of it for users who want a full-featured client without the DIY.

---

## Features

### ✅ Auto-Replace Stuck Downloads
*First feature unique to slskdn!*

Downloads get stuck. Users go offline. Transfers time out. Instead of manually searching for alternatives, slskdn does it automatically:

- Detects stuck downloads (timed out, errored, rejected, cancelled)
- Searches the network for alternative sources
- Filters by file extension and configurable size threshold (default 5%)
- Ranks alternatives by size match, free slots, queue depth, and speed
- Automatically cancels the stuck download and enqueues the best alternative

**Enable via UI toggle or CLI:**
```bash
slskd --auto-replace-stuck --auto-replace-threshold 5.0
```

### 🔜 Coming Soon

| Feature | Status | Description |
|---------|--------|-------------|
| **Wishlist/Background Search** | Planned | Save searches that run automatically |
| **Clear All Searches** | Planned | One-click cleanup |
| **Smart Result Ranking** | Planned | Rank results by download history |
| **User History Badges** | Planned | See past downloads per user |
| **Block Users from Search** | Planned | Hide scammers/fake results |
| **Multiple Download Destinations** | Planned | Choose folder per download |

See the full [Feature Roadmap](FORK_VISION.md) for all planned features.

---

## Quick Start

### With Docker

```bash
docker run -d \
  -p 5030:5030 \
  -p 50300:50300 \
  -e SLSKD_SLSK_USERNAME=your_username \
  -e SLSKD_SLSK_PASSWORD=your_password \
  -v /path/to/downloads:/downloads \
  -v /path/to/app:/app \
  --name slskdn \
  ghcr.io/snapetech/slskdn:latest
```

### With Docker Compose

```yaml
version: "3"
services:
  slskdn:
    image: ghcr.io/snapetech/slskdn:latest
    container_name: slskdn
    ports:
      - "5030:5030"    # Web UI
      - "50300:50300"  # Soulseek listen port
    environment:
      - SLSKD_SLSK_USERNAME=your_username
      - SLSKD_SLSK_PASSWORD=your_password
      - SLSKD_REMOTE_CONFIGURATION=true
    volumes:
      - ./app:/app
      - ./downloads:/downloads
      - ./music:/music:ro  # Read-only share
    restart: unless-stopped
```

### From Source

```bash
# Clone the repo
git clone https://github.com/snapetech/slskdn.git
cd slskdn

# Install .NET SDK 8.0 (if needed)
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0
export PATH="$HOME/.dotnet:$PATH"

# Run the backend
cd src/slskd
dotnet run

# In another terminal, run the frontend (for development)
cd src/web
npm install
npm start
```

---

## Configuration

slskdn uses the same configuration format as slskd. Create `slskd.yml` in your app directory:

```yaml
soulseek:
  username: your_username
  password: your_password
  listen_port: 50300

directories:
  downloads: /downloads
  incomplete: /downloads/incomplete

shares:
  directories:
    - /music

web:
  port: 5030
  authentication:
    username: admin
    password: change_me

# slskdn-specific options
global:
  download:
    auto_replace_stuck: true
    auto_replace_threshold: 5.0
    auto_replace_interval: 60
```

---

## Comparison with slskd

| Feature | slskd | slskdn |
|---------|-------|--------|
| Core Soulseek functionality | ✅ | ✅ |
| Web UI | ✅ | ✅ |
| REST API | ✅ | ✅ |
| Auto-replace stuck downloads | ❌ | ✅ |
| Wishlist/background search | ❌ | 🔜 |
| Smart result ranking | ❌ | 🔜 |
| User download history | ❌ | 🔜 |
| Clear all searches | ❌ | 🔜 |
| Multiple download destinations | ❌ | 🔜 |

---

## Contributing

We welcome contributions! Here's how to help:

1. **Pick an issue** from our [Issue Tracker](https://github.com/snapetech/slskdn/issues)
2. **Fork the repo** and create a feature branch
3. **Submit a PR** with your changes

Priority areas:
- Features from the [roadmap](FORK_VISION.md)
- Bug fixes
- Documentation
- UI/UX improvements

### Development Setup

```bash
# Backend (C#/.NET 8)
cd src/slskd
dotnet watch run

# Frontend (React)
cd src/web
npm install
npm start
```

---

## Upstream Contributions

Features that prove stable in slskdn will be submitted as PRs to upstream slskd. Our auto-replace feature was the first: [slskd PR #1553](https://github.com/slskd/slskd/pull/1553).

We aim to be a **proving ground**, not a permanent fork.

---

## License

slskdn is licensed under the [GNU Affero General Public License v3.0](LICENSE), the same as slskd.

---

## Acknowledgments

- [slskd](https://github.com/slskd/slskd) - The excellent foundation we're building on
- [Soulseek.NET](https://github.com/jpdillingham/Soulseek.NET) - The .NET Soulseek library
- The Soulseek community

---

<p align="center">
  <strong>slskdn</strong> — Because "just write a script" isn't always the answer.
</p>
