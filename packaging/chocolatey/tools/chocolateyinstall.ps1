$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/snapetech/slskdn/releases/download/0.24.1-slskdn.32/slskdn-0.24.1-slskdn.32-win-x64.zip"
$checksum   = "5e8501af69aff9dda113d8589831d71df503dddfd5b7c6f4a2dbd1f82763696e"

Install-ChocolateyZipPackage -PackageName 'slskdn' -Url $url -UnzipLocation $toolsDir -Checksum $checksum -ChecksumType 'sha256'
