"use client";
import { useEffect,useState } from "react";
import { AuditViewer, StatusBadge, type AuditViewerItem } from "@/components/ui";
export function CompliancePanel(){const[audit,setAudit]=useState<AuditViewerItem[]>([]);useEffect(()=>{fetch("/api/v1/admin/audit").then(async r=>{if(r.ok)setAudit(await r.json() as AuditViewerItem[])})},[]);return <><div className="admin-grid"><Card title="Acessibilidade" status="TEST AUTOMATION READY"/><Card title="Auditoria" status="IMPLEMENTED"/><Card title="LGPD" status="POLICY + MINIMIZATION"/><Card title="Providers" status="STATUS EXPLICIT"/><Card title="Backups" status="INFRA EVIDENCE REQUIRED"/><Card title="Produção" status="NOT VALIDATED"/></div><section className="admin-panel"><h2>Últimos eventos de auditoria</h2><AuditViewer items={audit.slice(0,20)}/></section></>}
function Card({title,status}:{title:string;status:string}){return <div className="metric-card"><span>{title}</span><StatusBadge status={status}/></div>}
