import * as React from "react";
import { Helmet } from "react-helmet";
import { isGoogleAdsDisplayed } from "./GoogleAd";

const PageHeader = props => {
    if (isGoogleAdsDisplayed && (props.noindex || props.noad)) {
        // noindexのページにAdsenseの自動広告が引き継がれそうになった場合は、リロードして消す
        window.location.reload();
        return null;
    }

    return (
        <Helmet>
            {props.title && <title>{props.title}</title>}
            {props.desc && <meta name="description" content={props.desc} />}
            {props.noindex && <meta name="robots" content="noindex" />}
        </Helmet>
    );
};
export default PageHeader;
