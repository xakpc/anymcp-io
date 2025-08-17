import { readFileSync, readdirSync } from 'fs';
import { join, dirname, extname, basename } from 'path';
import { fileURLToPath } from 'url';
import { parse as parseYaml } from 'yaml';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

export default function() {
  const servers = {};
  
  // Load servers from C# files in mcp directory
  try {
    const mcpDir = join(__dirname, '../../mcp');
    console.log('Looking for C# files in:', mcpDir);
    const files = readdirSync(mcpDir).filter(f => extname(f) === '.cs');
    console.log('Found C# files:', files);
    
    for (const file of files) {
      const filePath = join(mcpDir, file);
      const contents = readFileSync(filePath, 'utf8');
      const server = parseCSharpMcpServer(contents, filePath);
      console.log('Parsed server:', server.id, server.name);
      servers[server.id] = server;
    }
  } catch (error) {
    console.warn('No mcp directory found or failed to read C# files:', error.message);
  }
  
  console.log('Total servers loaded:', Object.keys(servers).length);
  return servers;
}

function parseCSharpMcpServer(contents, filePath) {
  const lines = contents.split('\n');
  const frontMatterLines = [];
  let inFrontMatter = false;
  
  // Extract front matter from C# comments
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();
    
    if (line === '// ---') {
      if (!inFrontMatter) {
        inFrontMatter = true;
        continue;
      } else {
        break;
      }
    }
    
    if (inFrontMatter && line.startsWith('//')) {
      // Remove the // prefix and add to front matter
      const yamlLine = line.substring(2);
      // Preserve leading spaces for YAML indentation
      if (yamlLine.startsWith(' ')) {
        frontMatterLines.push(yamlLine);
      } else if (yamlLine.trim()) {
        frontMatterLines.push(yamlLine.trim());
      } else {
        frontMatterLines.push(''); // preserve empty lines
      }
    }
  }
  
  // Parse YAML front matter
  let metadata = {};
  if (frontMatterLines.length > 0) {
    try {
      const yamlContent = frontMatterLines.join('\n');
      metadata = parseYaml(yamlContent) || {};
    } catch (error) {
      console.warn(`Failed to parse front matter in ${filePath}:`, error.message);
      console.warn('YAML content:', frontMatterLines.join('\n'));
    }
  }
  
  // Extract filename for default ID
  const filename = basename(filePath, '.cs');
  
  // Extract code without front matter
  const codeWithoutFrontMatter = extractCodeWithoutFrontMatter(contents);
  
  // Extract tools from the C# code
  const tools = extractToolsFromCSharp(contents);
  
  // Build server object
  const server = {
    id: metadata.id || filename,
    name: metadata.name || `${filename}.cs`,
    description: metadata.description || 'MCP Server',
    longDescription: metadata.longDescription || metadata.description || 'MCP Server',
    tags: Array.isArray(metadata.tags) ? metadata.tags : (metadata.tags ? [metadata.tags] : []),
    status: metadata.status || 'stable',
    downloads: metadata.downloads || 0,
    lastUpdated: metadata.lastUpdated || new Date().toISOString().split('T')[0],
    version: metadata.version || '1.0.0',
    author: metadata.author || 'Unknown',
    license: metadata.license || 'MIT',
    createdDate: metadata.createdDate || metadata.lastUpdated || new Date().toISOString().split('T')[0],
    envVars: metadata.envVars || [],
    tools: tools,
    code: contents,
    displayCode: codeWithoutFrontMatter
  };
  
  return server;
}

function extractToolsFromCSharp(contents) {
  const tools = [];
  const lines = contents.split('\n');
  
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();
    
    // Look for McpServerTool attribute with Description
    if (line.includes('[McpServerTool') && line.includes('Description(')) {
      const descriptionMatch = line.match(/Description\("([^"]+)"\)/);
      if (descriptionMatch) {
        const description = descriptionMatch[1];
        
        // Look for the method signature in the next few lines
        for (let j = i + 1; j < Math.min(i + 10, lines.length); j++) {
          const methodLine = lines[j].trim();
          if (methodLine.includes('public static') && methodLine.includes('(')) {
            // Extract method name
            const methodMatch = methodLine.match(/public static\s+\w+\s+(\w+)\s*\(/);
            if (methodMatch) {
              const methodName = methodMatch[1];
              tools.push({
                name: methodName,
                description: description
              });
              break;
            }
          }
        }
      }
    }
  }
  
  return tools;
}

function extractCodeWithoutFrontMatter(contents) {
  const lines = contents.split('\n');
  let frontMatterEndIndex = -1;
  let frontMatterStartFound = false;
  
  // Find the end of front matter
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();
    
    if (line === '// ---') {
      if (!frontMatterStartFound) {
        frontMatterStartFound = true;
      } else {
        frontMatterEndIndex = i;
        break;
      }
    }
  }
  
  let code;
  // If front matter was found, return everything after it
  if (frontMatterEndIndex >= 0) {
    code = lines.slice(frontMatterEndIndex + 1).join('\n').trim();
  } else {
    // If no front matter found, return original content
    code = contents.trim();
  }
  
  // Decode HTML entities that might have been introduced during processing
  code = code
    .replace(/&quot;/g, '"')
    .replace(/&#39;/g, "'")
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&amp;/g, '&');
    
  return code;
}