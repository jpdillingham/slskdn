import { urlBase } from '../../config';
import * as slskdnAPI from '../../lib/slskdn';
import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Icon, Label } from 'semantic-ui-react';

const STORAGE_KEY = 'slskdn-status-bar-visible';

const formatNumber = (value) => {
  if (value === undefined || value === null) return '0';
  if (value >= 1_000_000) return (value / 1_000_000).toFixed(1) + 'M';
  if (value >= 1_000) return (value / 1_000).toFixed(1) + 'K';
  return value.toString();
};

// Export for use in App.jsx
export const isStatusBarVisible = () => {
  return localStorage.getItem(STORAGE_KEY) !== 'false';
};

export const toggleStatusBarVisibility = () => {
  const current = localStorage.getItem(STORAGE_KEY) !== 'false';
  localStorage.setItem(STORAGE_KEY, !current);
  window.dispatchEvent(new Event('slskdn-status-toggle'));
  return !current;
};

const SlskdnStatusBar = () => {
  const [stats, setStats] = useState({
    activeSwarms: 0,
    hashCount: 0,
    isSyncing: false,
    meshPeers: 0,
    seqId: 0,
  });
  const [visible, setVisible] = useState(() => isStatusBarVisible());

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const data = await slskdnAPI.getSlskdnStats();
        setStats({
          activeSwarms: data.swarmJobs?.length ?? 0,
          backfillActive: data.backfill?.isActive ?? false,
          hashCount: data.hashDb?.totalEntries ?? 0,
          isSyncing: data.mesh?.isSyncing ?? false,
          meshPeers: data.mesh?.connectedPeerCount ?? 0,
          seqId: data.hashDb?.currentSeqId ?? data.mesh?.localSeqId ?? 0,
        });
      } catch (error) {
        console.error('Failed to fetch slskdn stats for status bar:', error);
      }
    };

    fetchStats();
    const interval = setInterval(fetchStats, 10_000);

    // Listen for toggle events from nav bar
    const handleToggle = () => setVisible(isStatusBarVisible());
    window.addEventListener('slskdn-status-toggle', handleToggle);

    return () => {
      clearInterval(interval);
      window.removeEventListener('slskdn-status-toggle', handleToggle);
    };
  }, []);

  if (!visible) return null;

  const {
    activeSwarms,
    backfillActive,
    hashCount,
    isSyncing,
    meshPeers,
    seqId,
  } = stats;

  return (
    <div className="slskdn-status-bar">
      <div className="slskdn-status-items">
        <Link
          className="slskdn-status-item"
          title="Connected slskdn mesh peers"
          to={`${urlBase}/system/network`}
        >
          <Icon
            color={meshPeers > 0 ? 'green' : 'grey'}
            name="sitemap"
            size="small"
          />
          <span className={meshPeers > 0 ? 'active' : ''}>
            {meshPeers} mesh
          </span>
        </Link>

        <span className="slskdn-status-divider">│</span>

        <Link
          className="slskdn-status-item"
          title="Content hashes in local database"
          to={`${urlBase}/system/network`}
        >
          <Icon
            color={hashCount > 0 ? 'blue' : 'grey'}
            name="database"
            size="small"
          />
          <span className={hashCount > 0 ? 'active' : ''}>
            {formatNumber(hashCount)} hashes
          </span>
        </Link>

        <span className="slskdn-status-divider">│</span>

        <Link
          className="slskdn-status-item"
          title="Mesh sync sequence ID"
          to={`${urlBase}/system/network`}
        >
          <Icon
            className={isSyncing ? 'loading' : ''}
            color={isSyncing ? 'yellow' : 'grey'}
            name="sync"
            size="small"
          />
          <span className={isSyncing ? 'syncing' : ''}>seq:{seqId}</span>
        </Link>

        {activeSwarms > 0 && (
          <>
            <span className="slskdn-status-divider">│</span>
            <Link
              className="slskdn-status-item"
              title="Active swarm downloads"
              to={`${urlBase}/downloads`}
            >
              <Label
                color="yellow"
                size="mini"
              >
                <Icon name="bolt" />
                {activeSwarms} swarm{activeSwarms === 1 ? '' : 's'}
              </Label>
            </Link>
          </>
        )}

        {backfillActive && (
          <>
            <span className="slskdn-status-divider">│</span>
            <span
              className="slskdn-status-item"
              title="Backfill active"
            >
              <Icon
                color="purple"
                loading
                name="clock"
                size="small"
              />
              <span className="backfill">backfill</span>
            </span>
          </>
        )}
      </div>

      <div className="slskdn-status-right">
        <Link
          className="slskdn-status-link"
          title="View network dashboard"
          to={`${urlBase}/system/network`}
        >
          slskdn network →
        </Link>
        <button
          className="slskdn-status-close"
          onClick={() => {
            localStorage.setItem(STORAGE_KEY, 'false');
            setVisible(false);
            window.dispatchEvent(new Event('slskdn-status-toggle'));
          }}
          title="Hide status bar (click signal icon in nav to restore)"
          type="button"
        >
          ×
        </button>
      </div>
    </div>
  );
};

export default SlskdnStatusBar;
