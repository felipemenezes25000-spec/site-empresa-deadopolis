import Image, { type ImageProps } from "next/image";

const DEFAULT_VARIANT_WIDTHS = [480, 768, 1200] as const;

type ResponsiveMediaImageProps = Omit<ImageProps, "src" | "width" | "height"> & {
  src: string;
  width: number;
  height: number;
  sizes?: string;
  variantWidths?: readonly number[];
};

export function buildMediaVariantUrl(src: string, width: number, height: number, format: "webp" = "webp") {
  const [path, query] = src.split("?", 2);
  const preservedQuery = query ? `&${query}` : "";
  return `${path}/variant?width=${width}&height=${height}&format=${format}${preservedQuery}`;
}

export function ResponsiveMediaImage({
  src,
  width,
  height,
  sizes = "(max-width: 760px) 100vw, 1200px",
  variantWidths = DEFAULT_VARIANT_WIDTHS,
  alt,
  ...imageProps
}: ResponsiveMediaImageProps) {
  const aspectRatio = width / height;
  const webpSrcSet = variantWidths
    .map((variantWidth) => {
      const variantHeight = Math.max(64, Math.round(variantWidth / aspectRatio));
      return `${buildMediaVariantUrl(src, variantWidth, variantHeight)} ${variantWidth}w`;
    })
    .join(", ");

  return (
    <picture>
      <source type="image/webp" srcSet={webpSrcSet} sizes={sizes} />
      <Image
        {...imageProps}
        src={src}
        width={width}
        height={height}
        sizes={sizes}
        unoptimized
        alt={alt}
      />
    </picture>
  );
}
