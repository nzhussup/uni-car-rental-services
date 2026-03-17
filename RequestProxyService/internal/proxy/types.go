package proxy

type ExecuteRequest struct {
	Service string            `json:"service" binding:"required" example:"google-places"`
	Method  string            `json:"method" binding:"required" example:"GET"`
	Path    string            `json:"path" binding:"required" example:"/maps/api/place/details/json"`
	Headers map[string]string `json:"headers" example:"{\"X-Correlation-Id\":\"abc-123\"}"`
	Query   map[string]string `json:"query" example:"{\"place_id\":\"ChIJN1t_tDeuEmsRUsoyG83frY4\"}"`
	Body    any               `json:"body"`
}

type ErrorResponse struct {
	Error   string `json:"error" example:"service not defined"`
	Service string `json:"service,omitempty" example:"google-places"`
	Status  int    `json:"status,omitempty" example:"404"`
	Details string `json:"details,omitempty" example:"upstream returned non-2xx status"`
}
