FROM node:24-alpine AS dependencies
WORKDIR /app
COPY apps/web/package.json apps/web/package-lock.json ./
RUN npm ci

FROM node:24-alpine AS build
WORKDIR /app
COPY --from=dependencies /app/node_modules ./node_modules
COPY apps/web .
RUN npm run build

FROM node:24-alpine AS final
RUN addgroup -S municipal && adduser -S municipal -G municipal
WORKDIR /app
ENV NODE_ENV=production
COPY --from=build --chown=municipal:municipal /app/.next/standalone ./
COPY --from=build --chown=municipal:municipal /app/.next/static ./.next/static
COPY --from=build --chown=municipal:municipal /app/public ./public
USER municipal
EXPOSE 3000
CMD ["node", "server.js"]
