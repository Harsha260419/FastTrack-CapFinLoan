FROM node:20.19.0-alpine3.21 AS build
WORKDIR /app

ARG VITE_GOOGLE_CLIENT_ID
ENV VITE_GOOGLE_CLIENT_ID=$VITE_GOOGLE_CLIENT_ID

RUN apk upgrade --no-cache

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.27.5-alpine AS final
WORKDIR /usr/share/nginx/html
RUN apk upgrade --no-cache
COPY --from=build /app/dist .
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
