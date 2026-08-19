export default function(eleventyConfig) {
  // Note: Using client-side Prism.js instead of server-side highlighting

  // Copy static assets
  eleventyConfig.addPassthroughCopy("src/assets");
  
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

  // Map of server id -> displayCode, for the client-side copy and download scripts.
  // Only displayCode is needed in the browser, so the rest of each server is dropped.
  eleventyConfig.addFilter("displayCodeMap", function(servers) {
    const map = {};
    for (const [id, server] of Object.entries(servers || {})) {
      map[id] = server.displayCode;
    }
    return JSON.stringify(map);
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