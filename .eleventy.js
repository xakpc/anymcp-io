export default function(eleventyConfig) {
  // Note: Using client-side Prism.js instead of server-side highlighting

  // Copy static assets
  eleventyConfig.addPassthroughCopy("src/assets");
  eleventyConfig.addPassthroughCopy("public");
  
  // Add date filter
  eleventyConfig.addFilter("date", function(date, format) {
    if (!date) return '';
    const d = new Date(date);
    if (format === 'M/D/YYYY') {
      return `${d.getMonth() + 1}/${d.getDate()}/${d.getFullYear()}`;
    }
    return d.toLocaleDateString();
  });

  // Add locale string filter for numbers
  eleventyConfig.addFilter("localeString", function(num) {
    if (!num) return '';
    return num.toLocaleString();
  });

  // Add filter to decode HTML entities
  eleventyConfig.addFilter("decodeHtml", function(text) {
    if (!text) return '';
    return text
      .replace(/&quot;/g, '"')
      .replace(/&lt;/g, '<')
      .replace(/&gt;/g, '>')
      .replace(/&amp;/g, '&');
  });

  // Set up directory structure
  return {
    templateFormats: [
      "md",
      "njk",
      "html",
      "liquid"
    ],
    
    // Input directory
    dir: {
      input: "src",
      includes: "_includes",
      data: "_data",
      output: "_site"
    },
    
    // Use nunjucks for .html files
    htmlTemplateEngine: "njk",
    markdownTemplateEngine: "njk"
  };
};