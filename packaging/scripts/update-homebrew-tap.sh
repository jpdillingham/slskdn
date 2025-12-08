#!/bin/bash
set -e
VERSION=$1
LINUX_URL=$2
LINUX_SHA=$3
OSX_ARM_URL=$4
OSX_ARM_SHA=$5
OSX_INTEL_URL=$6
OSX_INTEL_SHA=$7
GH_TOKEN=$8

if [ -z "$GH_TOKEN" ]; then echo "GH_TOKEN not set"; exit 1; fi

git clone https://x-access-token:${GH_TOKEN}@github.com/snapetech/homebrew-slskdn.git tap-repo
cd tap-repo

mkdir -p Formula
cat > Formula/slskdn.rb <<RUBY
class Slskdn < Formula
  desc "🔋 The batteries-included fork of slskd. Feature-rich, including wishlist, smart ranking, tabbed browsing, notifications & more"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "${VERSION}"

  on_macos do
    on_arm do
      url "${OSX_ARM_URL}"
      sha256 "${OSX_ARM_SHA}"
    end
    on_intel do
      url "${OSX_INTEL_URL}"
      sha256 "${OSX_INTEL_SHA}"
    end
  end

  on_linux do
    url "${LINUX_URL}"
    sha256 "${LINUX_SHA}"
  end

  def install
    libexec.install Dir["*"]
    (bin/"slskdn").write_exec_script libexec/"slskd"
  end

  test do
    assert_match "slskd", shell_output("#{bin}/slskdn --help", 1)
  end
end
RUBY

git config user.name "slskdn-bot"
git config user.email "slskdn@proton.me"
git add Formula/slskdn.rb
git commit -m "Update slskdn to ${VERSION}"
git push origin main
