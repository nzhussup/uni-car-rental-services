package config

import (
	"os"
	"path/filepath"
	"testing"
)

func TestLoadServices(t *testing.T) {
	t.Parallel()

	tmpDir := t.TempDir()
	configPath := filepath.Join(tmpDir, "services.json")
	content := `[
		{
			"id": "test-service",
			"baseUrl": "https://example.com",
			"allowedMethods": ["get", "post"]
		}
	]`

	if err := os.WriteFile(configPath, []byte(content), 0o600); err != nil {
		t.Fatalf("write config: %v", err)
	}

	services, err := LoadServices(configPath)
	if err != nil {
		t.Fatalf("LoadServices() error = %v", err)
	}

	svc, ok := services["test-service"]
	if !ok {
		t.Fatalf("expected service to be loaded")
	}

	if len(svc.AllowedMethods) != 2 || svc.AllowedMethods[0] != "GET" || svc.AllowedMethods[1] != "POST" {
		t.Fatalf("expected uppercase methods, got %#v", svc.AllowedMethods)
	}
}

func TestLoadServices_ValidationError(t *testing.T) {
	t.Parallel()

	tmpDir := t.TempDir()
	configPath := filepath.Join(tmpDir, "services.json")
	content := `[
		{
			"id": "",
			"baseUrl": "https://example.com",
			"allowedMethods": ["GET"]
		}
	]`

	if err := os.WriteFile(configPath, []byte(content), 0o600); err != nil {
		t.Fatalf("write config: %v", err)
	}

	if _, err := LoadServices(configPath); err == nil {
		t.Fatalf("expected validation error, got nil")
	}
}
