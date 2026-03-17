package main

import (
	"log"
	"net/http"
	"os"
	"path/filepath"
	"time"

	"request-proxy-service/internal/config"
	"request-proxy-service/internal/proxy"

	"github.com/gin-gonic/gin"
	"github.com/joho/godotenv"
)

// @title Request Proxy Service API
// @version 1.0
// @description Config-driven proxy API for calling whitelisted external services without exposing secrets to frontend clients.
// @tag.name RequestProxyService
// @tag.description Generic proxy endpoints for configured external service calls.
// @BasePath /
func main() {
	loadLocalEnvFiles()

	configPath := getEnvOrDefault("SERVICES_CONFIG_PATH", "config/services.json")
	port := getEnvOrDefault("PORT", "8080")

	services, err := config.LoadServices(configPath)
	if err != nil {
		log.Fatalf("failed to load services config: %v", err)
	}

	router := gin.Default()
	handler := proxy.NewHandler(services)

	router.GET("/health", func(c *gin.Context) {
		c.JSON(http.StatusOK, gin.H{"status": "ok"})
	})

	router.POST("/api/proxy/execute", handler.Execute)

	server := &http.Server{
		Addr:              ":" + port,
		Handler:           router,
		ReadHeaderTimeout: 5 * time.Second,
	}

	log.Printf("request proxy service started on :%s (config: %s)", port, absPath(configPath))
	if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		log.Fatalf("server stopped with error: %v", err)
	}
}

func loadLocalEnvFiles() {
	// Load local env files for developer convenience, but keep exported shell env vars authoritative.
	_ = godotenv.Load(".env.local", ".env")
}

func getEnvOrDefault(key, fallback string) string {
	value := os.Getenv(key)
	if value == "" {
		return fallback
	}
	return value
}

func absPath(path string) string {
	abs, err := filepath.Abs(path)
	if err != nil {
		return path
	}
	return abs
}
