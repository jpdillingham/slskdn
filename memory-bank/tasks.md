# Tasks (Source of Truth)

> This file is the canonical task list for slskdN development.  
> AI agents should add/update tasks here, not invent ephemeral todos in chat.

---

## Active Development

### High Priority

- [ ] **T-001**: Persistent Room/Chat Tabs
  - Status: Not started
  - Priority: High
  - Branch: TBD
  - Related: `TODO.md`, Browse tabs implementation
  - Notes: Implement tabbed interface like Browse currently has. Reuse `Browse.jsx`/`BrowseSession.jsx` patterns.

- [ ] **T-002**: Scheduled Rate Limits
  - Status: Not started
  - Priority: High
  - Branch: TBD
  - Related: slskd #985
  - Notes: Day/night upload/download speed schedules like qBittorrent

### Medium Priority

- [ ] **T-003**: Download Queue Position Polling
  - Status: Not started
  - Priority: Medium
  - Related: slskd #921
  - Notes: Auto-refresh queue positions for queued files

- [ ] **T-004**: Visual Group Indicators
  - Status: Not started
  - Priority: Medium
  - Related: slskd #745
  - Notes: Icons in search results for users in your groups

- [ ] **T-005**: Traffic Ticker
  - Status: Not started
  - Priority: Medium
  - Related: slskd discussion #547
  - Notes: Real-time upload/download activity feed in UI

### Low Priority

- [ ] **T-006**: Create Chat Rooms from UI
  - Status: Not started
  - Priority: Low
  - Related: slskd #1258
  - Notes: Create public/private rooms from web interface

- [ ] **T-007**: Predictable Search URLs
  - Status: Not started
  - Priority: Low
  - Related: slskd #1170
  - Notes: Bookmarkable search URLs for browser integration

---

## Packaging & Distribution

- [ ] **T-010**: TrueNAS SCALE Apps
  - Status: Not started
  - Priority: High
  - Notes: Helm chart or ix-chart format

- [ ] **T-011**: Synology Package Center
  - Status: Not started
  - Priority: High
  - Notes: SPK format, cross-compile for ARM/x86

- [ ] **T-012**: Homebrew Formula
  - Status: Not started
  - Priority: High
  - Notes: macOS package manager support

- [ ] **T-013**: Flatpak (Flathub)
  - Status: Not started
  - Priority: High
  - Notes: Universal Linux packaging

---

## Completed Tasks

### Multi-Source & DHT Infrastructure (experimental/multi-source-swarm)

- [x] **T-200**: Multi-Source Chunked Downloads
  - Status: Done (experimental branch)
  - Branch: experimental/multi-source-swarm
  - Notes: Parallel chunk downloads from multiple peers, content verification (SHA256), FLAC STREAMINFO parser, LimitedWriteStream for partial downloads

- [x] **T-201**: BitTorrent DHT Rendezvous Layer
  - Status: Done (experimental branch)
  - Branch: experimental/multi-source-swarm
  - Notes: Decentralized peer discovery using BitTorrent DHT, beacon/seeker model, overlay TCP connections, TLS 1.3 encrypted mesh

- [x] **T-202**: Mesh Overlay Network & Hash Sync
  - Status: Done (experimental branch)
  - Branch: experimental/multi-source-swarm
  - Notes: Epidemic sync protocol for hash database, TLS-encrypted P2P connections, certificate pinning (TOFU), SecureMessageFramer with length-prefixed framing

- [x] **T-203**: Capability Discovery System
  - Status: Done (experimental branch)
  - Branch: experimental/multi-source-swarm
  - Notes: PeerCapabilityFlags, UserInfo tag parsing, version detection, REST API endpoints

- [x] **T-204**: Local Hash Database (HashDb)
  - Status: Done (experimental branch)
  - Branch: experimental/multi-source-swarm
  - Notes: SQLite-based content-addressed hash storage, FLAC inventory, peer tracking, mesh peer state

- [x] **T-205**: Security Hardening Framework
  - Status: Done (experimental branch)
  - Branch: experimental/multi-source-swarm
  - Notes: NetworkGuard rate limiting, ViolationTracker auto-bans, PathGuard traversal prevention, PeerReputation scoring, ContentSafety magic bytes, ByzantineConsensus voting, EntropyMonitor, FingerprintDetection, Honeypots

- [x] **T-206**: Source Discovery & Verification
  - Status: Done (experimental branch)
  - Branch: experimental/multi-source-swarm
  - Notes: Automatic peer discovery for identical files, content verification service, FLAC audio MD5 matching, multi-source controller API

### HashDb & Passive Discovery (dev-2025-12-09)

- [x] **T-110**: HashDb Schema Migration System
  - Status: Done (dev-2025-12-09)
  - Branch: experimental/multi-source-swarm
  - Notes: Versioned SQLite migrations for HashDb, extends schema for full file hashes, audio fingerprints, MusicBrainz IDs, and FileSources table

- [x] **T-111**: Passive FLAC Discovery & Backfill
  - Status: Done (dev-2025-12-09)
  - Branch: experimental/multi-source-swarm
  - Notes: Passively discover FLACs from search results, peer interactions. Manual backfill UI with pagination. Network-health-first design.

- [x] **T-112**: UI Polish - Sticky Status Bar & Footer
  - Status: Done (dev-2025-12-09)
  - Branch: experimental/multi-source-swarm
  - Notes: Status bar fixed below nav, opaque colorful footer with parent project attribution, appears on all pages including login

- [x] **T-113**: Release Notes & AUR Checksum Fix
  - Status: Done (dev-2025-12-09)
  - Branch: experimental/multi-source-swarm
  - Notes: Established convention for release notes on GitHub releases. Fixed AUR PKGBUILD to keep SKIP for binary checksums to prevent yay -Syu validation failures.

### Stable Releases (main branch)

- [x] **T-100**: Auto-Replace Stuck Downloads
  - Status: Done (Release .1)
  - Notes: Finds alternatives for stuck/failed downloads

- [x] **T-101**: Wishlist/Background Search
  - Status: Done (Release .2)
  - Notes: Save searches, auto-run, auto-download

- [x] **T-102**: Smart Result Ranking
  - Status: Done (Release .4)
  - Notes: Speed, queue, slots, history weighted

- [x] **T-103**: User Download History Badge
  - Status: Done (Release .4)
  - Notes: Green/blue/orange badges

- [x] **T-104**: Advanced Search Filters
  - Status: Done (Release .5)
  - Notes: Modal with include/exclude, size, bitrate

- [x] **T-105**: Block Users from Search Results
  - Status: Done (Release .5)
  - Notes: Hide blocked users toggle

- [x] **T-106**: User Notes & Ratings
  - Status: Done (Release .6)
  - Notes: Personal notes per user

- [x] **T-107**: Multiple Destination Folders
  - Status: Done (Release .2)
  - Notes: Choose destination per download

- [x] **T-108**: Tabbed Browse Sessions
  - Status: Done (Release .10)
  - Notes: Multiple browse tabs, persistent

- [x] **T-109**: Push Notifications
  - Status: Done (Release .8)
  - Notes: Ntfy, Pushover, Pushbullet

---

*Last updated: December 9, 2025*

