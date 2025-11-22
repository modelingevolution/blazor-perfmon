#!/bin/bash

# Build multi-platform Docker images for Blazor Performance Monitor Example
# Supports x64 (amd64) and ARM64 (arm64) architectures

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
IMAGE_NAME="modelingevolution/blazor-perfmon-example"
TAG="latest"
PLATFORMS="linux/amd64,linux/arm64"
PUSH=false
LOAD=false

# Display usage
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Build multi-platform Docker images for the Performance Monitor example"
    echo ""
    echo "Options:"
    echo "  -t, --tag TAG          Image tag (default: latest)"
    echo "  -p, --push             Push images to registry"
    echo "  -l, --load             Load image to local Docker (single platform only)"
    echo "  --platform PLATFORMS   Comma-separated list of platforms (default: linux/amd64,linux/arm64)"
    echo "  --amd64-only          Build for AMD64 only"
    echo "  --arm64-only          Build for ARM64 only"
    echo "  -h, --help            Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                              # Build for all platforms"
    echo "  $0 -t v1.0.0 -p                # Build and push with tag v1.0.0"
    echo "  $0 --amd64-only -l             # Build for AMD64 and load to local Docker"
    echo "  $0 -t dev --arm64-only         # Build for ARM64 with dev tag"
    exit 1
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -t|--tag)
            TAG="$2"
            shift 2
            ;;
        -p|--push)
            PUSH=true
            shift
            ;;
        -l|--load)
            LOAD=true
            shift
            ;;
        --platform)
            PLATFORMS="$2"
            shift 2
            ;;
        --amd64-only)
            PLATFORMS="linux/amd64"
            shift
            ;;
        --arm64-only)
            PLATFORMS="linux/arm64"
            shift
            ;;
        -h|--help)
            usage
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            usage
            ;;
    esac
done

# Validate options
if [ "$PUSH" = true ] && [ "$LOAD" = true ]; then
    echo -e "${RED}Error: Cannot use --push and --load together${NC}"
    exit 1
fi

if [ "$LOAD" = true ] && [[ "$PLATFORMS" == *","* ]]; then
    echo -e "${RED}Error: --load can only be used with a single platform${NC}"
    echo "Use --amd64-only or --arm64-only with --load"
    exit 1
fi

# Navigate to repository root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"

echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Building Blazor Performance Monitor Docker Images            ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${GREEN}Configuration:${NC}"
echo "  Image Name:  $IMAGE_NAME"
echo "  Tag:         $TAG"
echo "  Platforms:   $PLATFORMS"
echo "  Push:        $PUSH"
echo "  Load:        $LOAD"
echo "  Build Context: $REPO_ROOT"
echo ""

# Check if Docker buildx is available
if ! docker buildx version &> /dev/null; then
    echo -e "${RED}Error: docker buildx is not available${NC}"
    echo "Install buildx: https://docs.docker.com/buildx/working-with-buildx/"
    exit 1
fi

# Create or use buildx builder
BUILDER_NAME="perfmon-builder"
if ! docker buildx inspect "$BUILDER_NAME" &> /dev/null; then
    echo -e "${YELLOW}Creating buildx builder: $BUILDER_NAME${NC}"
    docker buildx create --name "$BUILDER_NAME" --use --driver docker-container --bootstrap
else
    echo -e "${GREEN}Using existing buildx builder: $BUILDER_NAME${NC}"
    docker buildx use "$BUILDER_NAME"
fi

# Build command
BUILD_ARGS="--platform $PLATFORMS"
BUILD_ARGS="$BUILD_ARGS -t $IMAGE_NAME:$TAG"
BUILD_ARGS="$BUILD_ARGS -f examples/ModelingEvolution.BlazorPerfMon.Example/Dockerfile"
BUILD_ARGS="$BUILD_ARGS ."

if [ "$PUSH" = true ]; then
    BUILD_ARGS="$BUILD_ARGS --push"
    echo -e "${YELLOW}Building and pushing to registry...${NC}"
elif [ "$LOAD" = true ]; then
    BUILD_ARGS="$BUILD_ARGS --load"
    echo -e "${YELLOW}Building and loading to local Docker...${NC}"
else
    echo -e "${YELLOW}Building images (not pushing or loading)...${NC}"
fi

# Build
echo ""
echo -e "${BLUE}Running: docker buildx build $BUILD_ARGS${NC}"
echo ""

if docker buildx build $BUILD_ARGS; then
    echo ""
    echo -e "${GREEN}✓ Build completed successfully!${NC}"
    echo ""

    if [ "$PUSH" = true ]; then
        echo -e "${GREEN}Images pushed to registry:${NC}"
        for platform in ${PLATFORMS//,/ }; do
            echo "  - $IMAGE_NAME:$TAG ($platform)"
        done
    elif [ "$LOAD" = true ]; then
        echo -e "${GREEN}Image loaded to local Docker:${NC}"
        echo "  - $IMAGE_NAME:$TAG ($PLATFORMS)"
        echo ""
        echo "Run with: docker run -p 5000:8080 $IMAGE_NAME:$TAG"
    else
        echo -e "${YELLOW}Images built but not pushed or loaded${NC}"
        echo "Add --push to push to registry or --load to load locally"
    fi

    echo ""
    echo -e "${BLUE}Test the image:${NC}"
    echo "  docker-compose up -d"
    echo ""
    echo -e "${BLUE}Or run manually:${NC}"
    echo "  docker run -p 5000:8080 \\"
    echo "    -v /var/run/docker.sock:/var/run/docker.sock:ro \\"
    echo "    -v /proc:/host/proc:ro \\"
    echo "    --cap-add SYS_ADMIN \\"
    echo "    $IMAGE_NAME:$TAG"
    echo ""
else
    echo ""
    echo -e "${RED}✗ Build failed${NC}"
    exit 1
fi
