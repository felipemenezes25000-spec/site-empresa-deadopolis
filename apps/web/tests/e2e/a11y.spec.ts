import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";
for(const route of ["/","/servicos","/transparencia","/ouvidoria","/admin/login"]){test(`axe sem violações serious/critical: ${route}`,async({page})=>{await page.goto(route);const results=await new AxeBuilder({page}).analyze();const severe=results.violations.filter(item=>item.impact==="serious"||item.impact==="critical");expect(severe.map(item=>({id:item.id,impact:item.impact,nodes:item.nodes.length}))).toEqual([])})}
