import api from './api';

// Capabilities API
export const getCapabilities = async () => {
  return (await api.get('/capabilities')).data;
};

export const getDiscoveredPeers = async () => {
  return (await api.get('/capabilities/peers')).data;
};

// HashDatabase API
export const getHashDatabaseStats = async () => {
  return (await api.get('/hashdb/stats')).data;
};

export const getHashDatabaseEntries = async (limit = 100, offset = 0) => {
  return (await api.get(`/hashdb/entries?limit=${limit}&offset=${offset}`))
    .data;
};

// Mesh API
export const getMeshStats = async () => {
  return (await api.get('/mesh/stats')).data;
};

export const getMeshPeers = async () => {
  return (await api.get('/mesh/peers')).data;
};

export const triggerMeshSync = async (username) => {
  return (await api.post(`/mesh/sync/${username}`)).data;
};

// Backfill API
export const getBackfillStats = async () => {
  return (await api.get('/backfill/stats')).data;
};

export const getBackfillCandidates = async (limit = 50) => {
  return (await api.get(`/backfill/candidates?limit=${limit}`)).data;
};

// MultiSource API
export const getActiveSwarmJobs = async () => {
  return (await api.get('/multisource/jobs')).data;
};

export const getSwarmJob = async (jobId) => {
  return (await api.get(`/multisource/jobs/${jobId}`)).data;
};

// Combined stats fetch for dashboard
export const getSlskdnStats = async () => {
  try {
    const [capabilities, hashDatabase, mesh, backfill, swarmJobs] =
      await Promise.allSettled([
        getCapabilities(),
        getHashDatabaseStats(),
        getMeshStats(),
        getBackfillStats(),
        getActiveSwarmJobs(),
      ]);

    return {
      backfill: backfill.status === 'fulfilled' ? backfill.value : null,
      capabilities:
        capabilities.status === 'fulfilled' ? capabilities.value : null,
      hashDb: hashDatabase.status === 'fulfilled' ? hashDatabase.value : null,
      mesh: mesh.status === 'fulfilled' ? mesh.value : null,
      swarmJobs: swarmJobs.status === 'fulfilled' ? swarmJobs.value : [],
    };
  } catch (error) {
    console.error('Failed to fetch slskdn stats:', error);
    return {
      backfill: null,
      capabilities: null,
      hashDb: null,
      mesh: null,
      swarmJobs: [],
    };
  }
};
