FROM node:20

WORKDIR /app
ENV PATH /app/node_modules/.bin:$PATH

COPY package.json package-lock.json* ./
RUN npm ci --silent

COPY . .

EXPOSE 19000 19001 19002

CMD ["sh", "-c", "npm run start -- --host lan"]
