#!/bin/zsh
# Builds the mobile store packages into release/:
#   release/android/*.aab  (upload to Google Play)
#   release/android/*.apk  (for side-loading / testing)
#   release/ios/*.ipa      (upload to App Store Connect)
# Android signing uses ~/keys/goldeneagle-upload.jks; its password is read from the macOS Keychain
# (service "GoldenEagle Android upload keystore"). iOS signing uses "Apple Distribution: Paul Johnson
# (3UH7BE38T3)" and the rel-eagle profile (~/Downloads/releagle.mobileprovision, installed by this script).
#
#   tools/build_release.sh        Android and iOS
#   tools/build_release.sh ios    iOS only (no Android upload key needed)
set -euo pipefail
cd "$(dirname "$0")/.."

# Xcode's tools find provisioning profiles by name in these folders.
for f in releagle develeagle; do
  [[ -f ~/Downloads/$f.mobileprovision ]] || continue
  uuid=$(security cms -D -i ~/Downloads/$f.mobileprovision | plutil -extract UUID raw -)
  for dir in "$HOME/Library/MobileDevice/Provisioning Profiles" "$HOME/Library/Developer/Xcode/UserData/Provisioning Profiles"; do
    mkdir -p "$dir" && cp ~/Downloads/$f.mobileprovision "$dir/$uuid.mobileprovision"
  done
done

rm -rf release/ios
dotnet publish GoldenEagle.iOS/GoldenEagle.iOS.csproj -c Release -f net10.0-ios -o release/ios
if [[ ${1:-} == ios ]]; then
  ls -la release/ios/*.ipa
  exit 0
fi

export GOLDENEAGLE_KEYSTORE_PASS="$(security find-generic-password -a goldeneagle-upload -s 'GoldenEagle Android upload keystore' -w)"
SDK="${ANDROID_HOME:-$HOME/Library/Android/sdk}"

rm -rf release/android
for format in aab apk; do
  dotnet publish GoldenEagle.Android/GoldenEagle.Android.csproj -c Release -f net10.0-android \
    -p:AndroidPackageFormat=$format -p:AndroidSdkDirectory="$SDK" -o release/android/$format
done
mkdir -p release/android/out
mv release/android/aab/*-Signed.aab release/android/apk/*-Signed.apk release/android/out/
rm -rf release/android/aab release/android/apk
mv release/android/out/* release/android/ && rmdir release/android/out

ls -la release/android release/ios/*.ipa
