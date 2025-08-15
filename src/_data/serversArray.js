import servers from './servers.js';

export default function() {
  const serversData = servers();
  
  return Object.entries(serversData).map(([key, value]) => ({
    id: key,
    ...value
  }));
}