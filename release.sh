#!/bin/bash

# Blazor Performance Monitor Release Script
# Creates and pushes a git tag to trigger NuGet package publishing

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Display usage
usage() {
    echo "Usage: ./release.sh <version>"
    echo ""
    echo "Examples:"
    echo "  ./release.sh 1.0.0"
    echo "  ./release.sh 1.2.3-beta"
    echo ""
    echo "This will create and push tag 'perfmon/<version>' to trigger NuGet publishing."
    exit 1
}

# Check if version argument is provided
if [ -z "$1" ]; then
    echo -e "${RED}Error: Version number required${NC}"
    usage
fi

VERSION=$1
TAG_NAME="perfmon/${VERSION}"

# Validate version format (basic semver check)
if ! [[ $VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9]+)?$ ]]; then
    echo -e "${YELLOW}Warning: Version '$VERSION' doesn't follow semantic versioning (x.y.z or x.y.z-suffix)${NC}"
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Aborted."
        exit 1
    fi
fi

# Check if tag already exists
if git rev-parse "$TAG_NAME" >/dev/null 2>&1; then
    echo -e "${RED}Error: Tag '$TAG_NAME' already exists${NC}"
    echo "Use 'git tag -d $TAG_NAME' to delete it locally if needed"
    exit 1
fi

# Check for uncommitted changes
if ! git diff-index --quiet HEAD --; then
    echo -e "${YELLOW}Warning: You have uncommitted changes${NC}"
    git status --short
    echo ""
    read -p "Continue with release anyway? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Aborted. Commit your changes first."
        exit 1
    fi
fi

# Get current branch
CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD)

# Confirm release
echo ""
echo -e "${GREEN}Ready to release version ${VERSION}${NC}"
echo "  Tag:    $TAG_NAME"
echo "  Branch: $CURRENT_BRANCH"
echo "  Commit: $(git rev-parse --short HEAD)"
echo ""
echo "This will:"
echo "  1. Create tag '$TAG_NAME'"
echo "  2. Push tag to origin"
echo "  3. Trigger GitHub Actions to publish to NuGet.org"
echo ""
read -p "Proceed with release? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Release aborted."
    exit 1
fi

# Create the tag
echo ""
echo "Creating tag $TAG_NAME..."
git tag -a "$TAG_NAME" -m "Release version $VERSION"

# Push the tag
echo "Pushing tag to origin..."
git push origin "$TAG_NAME"

echo ""
echo -e "${GREEN}✓ Release $VERSION completed successfully!${NC}"
echo ""
echo "GitHub Actions will now build and publish the NuGet packages."
echo "Monitor progress at: https://github.com/modelingevolution/blazor-perfmon/actions"
echo ""
echo "Packages will be published as:"
echo "  - ModelingEvolution.PerformanceMonitor.Shared v$VERSION"
echo "  - ModelingEvolution.PerformanceMonitor.Server v$VERSION"
echo "  - ModelingEvolution.PerformanceMonitor.Client v$VERSION"
echo ""
