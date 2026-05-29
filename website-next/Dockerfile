# syntax=docker/dockerfile:1
FROM nginx:alpine

# replace stock config
RUN rm -f /etc/nginx/nginx.conf /etc/nginx/conf.d/default.conf

COPY ./nginx/nginx.conf /etc/nginx/nginx.conf
COPY ./nginx/conf.d /etc/nginx/conf.d

# pre-built Next.js static export
COPY ./out /usr/share/nginx/html

# pre-compress text assets so gzip_static can serve them
RUN find /usr/share/nginx/html -type f \
        \( -name '*.html' -o -name '*.css' -o -name '*.js' -o -name '*.mjs' \
           -o -name '*.json' -o -name '*.svg' -o -name '*.xml' -o -name '*.txt' \
           -o -name '*.map' -o -name '*.ico' -o -name '*.webmanifest' \) \
        -size +1024c \
        -exec gzip -9 -k -f {} \;

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget -q --spider http://127.0.0.1/ || exit 1
