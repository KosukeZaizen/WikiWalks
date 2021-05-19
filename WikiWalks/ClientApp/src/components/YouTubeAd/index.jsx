import React from "react";

export const YouTubeAd = ({ width }) => (
    <a
        href="http://www.youtube.com/channel/UCii35PcojqMUNkSRalUw35g?sub_confirmation=1"
        target="_blank"
        rel="noopener noreferrer nofollow"
    >
        <img
            src="https://lingualninja.blob.core.windows.net/lingual-storage/appsPublic/ad/ad1.png"
            alt="Lingual Ninja YouTube Channel"
            style={{
                width: width || "100%",
                height: "auto",
                margin: "7px 0",
            }}
        />
    </a>
);
