package soap

import "testing"

func TestNewClientFault(t *testing.T) {
	t.Parallel()

	fault := NewClientFault("invalid request")

	if fault.FaultCode != clientFaultCode {
		t.Fatalf("unexpected fault code: got=%q want=%q", fault.FaultCode, clientFaultCode)
	}
	if fault.FaultString != "invalid request" {
		t.Fatalf("unexpected fault string: got=%q", fault.FaultString)
	}
	if fault.Detail == nil || fault.Detail.Message != "invalid request" {
		t.Fatalf("unexpected fault detail: %+v", fault.Detail)
	}
}

func TestNewServerFault(t *testing.T) {
	t.Parallel()

	fault := NewServerFault("internal error")

	if fault.FaultCode != serverFaultCode {
		t.Fatalf("unexpected fault code: got=%q want=%q", fault.FaultCode, serverFaultCode)
	}
	if fault.FaultString != "internal error" {
		t.Fatalf("unexpected fault string: got=%q", fault.FaultString)
	}
	if fault.Detail == nil || fault.Detail.Message != "internal error" {
		t.Fatalf("unexpected fault detail: %+v", fault.Detail)
	}
}
