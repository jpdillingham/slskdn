#!/usr/bin/env bash
# Update Winget manifests for dev builds

set -e

VERSION="$1"
WIN_X64_URL="$2"
WIN_X64_SHA="$3"
WIN_ARM64_URL="$4"
WIN_ARM64_SHA="$5"

if [ -z "$VERSION" ] || [ -z "$WIN_X64_URL" ] || [ -z "$WIN_X64_SHA" ]; then
    echo "Usage: $0 <version> <win-x64-url> <win-x64-sha> [win-arm64-url] [win-arm64-sha]"
    exit 1
fi

# Convert version for Winget (dots only, no hyphens)
WINGET_VERSION=$(echo "$VERSION" | sed 's/-/./g')

MANIFEST_DIR="packaging/winget"
INSTALLER_FILE="$MANIFEST_DIR/snapetech.slskdn-dev.installer.yaml"
LOCALE_FILE="$MANIFEST_DIR/snapetech.slskdn-dev.locale.en-US.yaml"
VERSION_FILE="$MANIFEST_DIR/snapetech.slskdn-dev.yaml"

echo "Updating Winget manifests to version $WINGET_VERSION"

# Update installer manifest
cat > "$INSTALLER_FILE" << EOF
# yaml-language-server: \$schema=https://aka.ms/winget-manifest.installer.1.6.0.schema.json

PackageIdentifier: snapetech.slskdn-dev
PackageVersion: $WINGET_VERSION
InstallerType: zip
InstallerSwitches:
  Silent: ""
  SilentWithProgress: ""
UpgradeBehavior: install
ReleaseDate: $(date -u +%Y-%m-%d)
Installers:
  - Architecture: x64
    InstallerUrl: $WIN_X64_URL
    InstallerSha256: $WIN_X64_SHA
    NestedInstallerType: portable
    NestedInstallerFiles:
      - RelativeFilePath: slskd.exe
        PortableCommandAlias: slskdn-dev
ManifestType: installer
ManifestVersion: 1.6.0
EOF

# Update locale manifest
cat > "$LOCALE_FILE" << EOF
# yaml-language-server: \$schema=https://aka.ms/winget-manifest.defaultLocale.1.6.0.schema.json

PackageIdentifier: snapetech.slskdn-dev
PackageVersion: $WINGET_VERSION
PackageLocale: en-US
Publisher: slskdN Team
PublisherUrl: https://github.com/snapetech
PublisherSupportUrl: https://github.com/snapetech/slskdn/issues
PackageName: slskdN (Development)
PackageUrl: https://github.com/snapetech/slskdn
License: AGPL-3.0-or-later
LicenseUrl: https://github.com/snapetech/slskdn/blob/main/LICENSE
ShortDescription: Batteries-included Soulseek web client (Development Build)
Description: |-
  slskdN is an experimental fork of slskd exploring advanced download features,
  protocol extensions, and network enhancements for Soulseek.
  
  ⚠️ WARNING: This is an unstable development build from the experimental branch.
  It includes cutting-edge features that may contain bugs.
  
  Features in development builds:
  - Multi-source swarm downloads
  - DHT mesh network with content verification
  - BitTorrent DHT rendezvous for peer discovery
  - TLS-secured mesh connections
  - Distributed hash database with mesh sync
  
  For stable releases, install 'snapetech.slskdn' instead.
Moniker: slskdn-dev
Tags:
  - soulseek
  - p2p
  - filesharing
  - music
  - experimental
ReleaseNotes: |-
  Development Build $VERSION
  
  This build includes experimental features from the multi-source-swarm branch.
  See https://github.com/snapetech/slskdn/releases/tag/dev for details.
ReleaseNotesUrl: https://github.com/snapetech/slskdn/releases/tag/dev
ManifestType: defaultLocale
ManifestVersion: 1.6.0
EOF

# Update version manifest
cat > "$VERSION_FILE" << EOF
# yaml-language-server: \$schema=https://aka.ms/winget-manifest.version.1.6.0.schema.json

PackageIdentifier: snapetech.slskdn-dev
PackageVersion: $WINGET_VERSION
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.6.0
EOF

echo "✅ Winget manifests updated:"
echo "  - $INSTALLER_FILE"
echo "  - $LOCALE_FILE"
echo "  - $VERSION_FILE"

