import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { StatusBadge, statusSeverity } from "./feedback";

describe("StatusBadge", () => {
  it("never paints a partial or demonstration state as a confirmed green state", () => {
    for (const status of ["DEMO_ONLY", "NOT_CONFIGURED", "DEGRADED", "QUARANTINED", "NOT_READY", "EXTERNAL_DEPENDENCY"]) {
      expect(statusSeverity(status), status).toBe("attention");
    }
  });

  it("paints unavailable, failed and revoked states as blocked", () => {
    for (const status of ["UNAVAILABLE", "FAILED", "REJECTED", "INACTIVE", "REVOKED", "UNHEALTHY"]) {
      expect(statusSeverity(status), status).toBe("blocked");
    }
  });

  it("reserves the confirmed state for capabilities that really work", () => {
    for (const status of ["AVAILABLE", "CONFIGURED", "IMPLEMENTED", "READY", "HEALTHY", "APPROVED", "RESOLVED"]) {
      expect(statusSeverity(status), status).toBe("ok");
    }
  });

  it("falls back to a neutral state instead of inventing a positive one", () => {
    expect(statusSeverity("OPEN")).toBe("neutral");
    expect(statusSeverity("ESTADO DESCONHECIDO")).toBe("neutral");
  });

  it("exposes the severity and the literal state to assistive technology", () => {
    render(<StatusBadge status="demo_only" />);
    const badge = screen.getByText("demo_only");
    expect(badge).toHaveAttribute("data-severity", "attention");
    expect(badge).toHaveAttribute("title", "situação exige atenção");
  });
});
