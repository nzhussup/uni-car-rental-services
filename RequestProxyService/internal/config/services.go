package config

import (
	"encoding/json"
	"fmt"
	"os"
	"strings"
)

type ServiceDefinition struct {
	ID             string            `json:"id"`
	BaseURL        string            `json:"baseUrl"`
	AllowedMethods []string          `json:"allowedMethods"`
	DefaultHeaders map[string]string `json:"defaultHeaders"`
	DefaultQuery   map[string]string `json:"defaultQuery"`
}

func LoadServices(path string) (map[string]ServiceDefinition, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("read services config: %w", err)
	}

	var defs []ServiceDefinition
	if err := json.Unmarshal(data, &defs); err != nil {
		return nil, fmt.Errorf("parse services config: %w", err)
	}

	services := make(map[string]ServiceDefinition, len(defs))
	for _, service := range defs {
		if service.ID == "" {
			return nil, fmt.Errorf("service id cannot be empty")
		}
		if service.BaseURL == "" {
			return nil, fmt.Errorf("service %q has empty baseUrl", service.ID)
		}
		if len(service.AllowedMethods) == 0 {
			return nil, fmt.Errorf("service %q must define at least one allowed method", service.ID)
		}

		normalizedMethods := make([]string, 0, len(service.AllowedMethods))
		for _, method := range service.AllowedMethods {
			normalizedMethods = append(normalizedMethods, strings.ToUpper(method))
		}
		service.AllowedMethods = normalizedMethods

		services[service.ID] = service
	}

	return services, nil
}
