import React, { Component } from "react";
import AnchorLink from "react-anchor-link-smooth-scroll";
import { connect } from "react-redux";
import { Link } from "react-router-dom";
import { Button } from "reactstrap";
import { bindActionCreators } from "redux";
import { actionCreators } from "../store/WikiWalks";
import GoogleAd from "./GoogleAd";
import Head from "./Helmet";
import { YouTubeAd } from "./YouTubeAd";

const patterns = {
    "&lt;": "<",
    "&gt;": ">",
    "&amp;": "&",
    "&quot;": '"',
    "&#x27;": "'",
    "&#x60;": "`",
};

class PagesForTheTitles extends Component {
    sectionStyle = {
        display: "block",
        borderTop: "1px solid #dcdcdc",
        paddingTop: 12,
        marginTop: 12,
        marginBottom: 30,
    };

    constructor(props) {
        super(props);

        this.state = {
            screenWidth: window.innerWidth,
        };

        let timer;
        window.onresize = () => {
            if (timer > 0) {
                clearTimeout(timer);
            }

            timer = setTimeout(() => {
                this.changeScreenSize();
            }, 100);
        };
    }

    changeScreenSize = () => {
        if (this.state.screenWidth !== window.innerWidth) {
            this.setState({
                screenWidth: window.innerWidth,
            });
        }
    };

    componentDidMount() {
        this.fetchData();
    }

    fetchData() {
        this.props.initialize();
        const wordId = this.props.match.params.wordId.split("#")[0];
        this.props.requestWord(wordId);
        this.props.requestCategoriesForTheTitle(wordId);
        this.props.requestPagesForTheTitle(wordId);
    }

    componentDidUpdate(previousProps) {
        const preLoc = previousProps.location.pathname.split("#")[0];
        const curLoc = this.props.location.pathname.split("#")[0];

        if (preLoc !== curLoc) {
            this.fetchData();
        }
    }

    render() {
        const isWide = this.state.screenWidth > 991;

        const wordId = Number(this.props.match.params.wordId.split("#")[0]);
        const { pages, categories: originCat } = this.props;
        const categories = originCat.filter(c =>
            c.category.toLowerCase().includes("japan")
        );
        const word = this.props.word || "Loading...";
        const cat =
            categories &&
            categories.sort((c1, c2) => c1.cnt - c2.cnt)[categories.length - 1];
        const category = cat && cat.category;
        const categoryForUrl =
            category && encodeURIComponent(category.split(" ").join("_"));
        let description = `This is a list of Wikipedia pages about ${word}. Pages mentioned about ${word} and pages related to ${word} are introduced.`;
        if (pages) {
            const page = pages.find(p => p.wordId === wordId);
            if (page) {
                if (page.snippet) {
                    const snippet =
                        Object.keys(patterns).reduce((acc, key) => {
                            return acc.split(key).join(patterns[key]);
                        }, page.snippet) + "...";
                    description = `List of pages about "${word}" in Wikipedia: "${snippet}"`;
                }
            }
        }

        const lineChangeDesc = (
            <div style={{ lineHeight: 1.7 }}>
                {"This is a list of Wikipedia pages about "}
                <span style={{ fontWeight: "bold" }}>{word}</span>
                {"."}
                <br />
                {"Pages mentioned about "}
                <span style={{ fontWeight: "bold" }}>{word}</span>
                {" and pages related to "}
                <span style={{ fontWeight: "bold" }}>{word}</span>
                {" are introduced."}
            </div>
        );
        const showAd = pages && pages.length > 50;

        return (
            <div>
                <Head
                    title={word}
                    desc={description}
                    noindex={categories.length <= 0}
                />
                <div
                    className="breadcrumbs"
                    itemScope
                    itemType="https://schema.org/BreadcrumbList"
                    style={{ textAlign: "left" }}
                >
                    <span
                        itemProp="itemListElement"
                        itemScope
                        itemType="http://schema.org/ListItem"
                    >
                        <Link
                            to="/"
                            itemProp="item"
                            style={{ marginRight: "5px", marginLeft: "5px" }}
                        >
                            <span itemProp="name">{"Home"}</span>
                        </Link>
                        <meta itemProp="position" content="1" />
                    </span>
                    {" > "}
                    {category ? (
                        <span
                            itemProp="itemListElement"
                            itemScope
                            itemType="http://schema.org/ListItem"
                        >
                            <Link
                                to={"/category/" + categoryForUrl}
                                itemProp="item"
                                style={{
                                    marginRight: "5px",
                                    marginLeft: "5px",
                                }}
                            >
                                <span itemProp="name">{category}</span>
                                <meta itemProp="position" content="2" />
                            </Link>
                        </span>
                    ) : (
                        word && (
                            <span
                                itemProp="itemListElement"
                                itemScope
                                itemType="http://schema.org/ListItem"
                            >
                                <Link
                                    to={"/all"}
                                    itemProp="item"
                                    style={{
                                        marginRight: "5px",
                                        marginLeft: "5px",
                                    }}
                                >
                                    <span itemProp="name">
                                        {"All Keywords"}
                                    </span>
                                    <meta itemProp="position" content="2" />
                                </Link>
                            </span>
                        )
                    )}
                    {" > "}
                    <span
                        itemProp="itemListElement"
                        itemScope
                        itemType="http://schema.org/ListItem"
                    >
                        <span
                            itemProp="name"
                            style={{ marginRight: "5px", marginLeft: "5px" }}
                        >
                            {word}
                        </span>
                        <meta itemProp="position" content="3" />
                    </span>
                </div>
                <article style={this.sectionStyle}>
                    <h1>{word}</h1>
                    <br />
                    {lineChangeDesc}
                    <span id="indexOfVocabLists"></span>
                    <br />
                    <div style={isWide ? { display: "flex" } : {}}>
                        {categories && categories.length > 0 && (
                            <div
                                style={{
                                    maxWidth: 500,
                                    padding: 10,
                                    marginBottom: isWide ? 20 : 30,
                                    marginRight: isWide ? 20 : 0,
                                    border: "5px double gray",
                                    width: "100%",
                                }}
                            >
                                <center>
                                    <p
                                        style={{
                                            fontWeight: "bold",
                                            fontSize: "large",
                                        }}
                                    >
                                        Index
                                    </p>
                                </center>
                                {word ? (
                                    <ul
                                        style={{
                                            ...this.sectionStyle,
                                            marginBottom: 0,
                                        }}
                                    >
                                        <li>
                                            <AnchorLink
                                                href={`#Pages about ${word}`}
                                            >{`Pages about ${word}`}</AnchorLink>
                                        </li>
                                        {categories.map((c, i) => (
                                            <li key={i}>
                                                <AnchorLink
                                                    href={"#" + c.category}
                                                >
                                                    {c.category}
                                                </AnchorLink>
                                            </li>
                                        ))}
                                    </ul>
                                ) : (
                                    <center>
                                        Loading...
                                        <br />
                                    </center>
                                )}
                            </div>
                        )}
                        <div
                            style={{
                                maxWidth: 400,
                                marginBottom: isWide ? 0 : 30,
                                ...(isWide
                                    ? { position: "relative", top: -10 }
                                    : {}),
                            }}
                        >
                            <YouTubeAd />
                        </div>
                    </div>
                    <section style={this.sectionStyle}>
                        <h2
                            id={`Pages about ${word}`}
                        >{`Pages about ${word}`}</h2>
                        {renderTable(pages, wordId, word)}
                    </section>
                    {showAd && <GoogleAd />}
                    {categories &&
                        categories.length > 0 &&
                        categories.map(c => (
                            <RenderOtherTable
                                key={c.category}
                                c={c}
                                wordId={wordId}
                                sectionStyle={this.sectionStyle}
                                pagesLoaded={pages && pages.length > 1}
                            />
                        ))}
                    {categories && categories.length > 0 && (
                        <React.Fragment>
                            <ReturnToIndex
                                refForReturnToIndex={this.refForReturnToIndex}
                                criteriaId={`Pages about ${word}`}
                            />
                            {showAd && categories.length > 3 && <GoogleAd />}
                            <div style={{ height: "50px" }}></div>
                        </React.Fragment>
                    )}
                </article>
            </div>
        );
    }
}

function renderTable(pages, wordId, word) {
    const pageLoaded = pages && pages.length > 0;

    const data =
        pageLoaded &&
        pages
            .sort(
                (page1, page2) =>
                    page2.snippet.split(word).length -
                    page1.snippet.split(word).length
            )
            .sort((page1, page2) => {
                if (page2.wordId === wordId) {
                    return 1;
                } else if (page1.wordId === wordId) {
                    return -1;
                } else {
                    return 0;
                }
            })
            .map(page => {
                return (
                    <tr key={page.wordId}>
                        <td
                            style={{
                                fontWeight: "bold",
                                minWidth: 120,
                                wordBreak: "normal",
                                wordWrap: "normal",
                            }}
                        >
                            {page.wordId !== wordId &&
                            page.referenceCount > 4 ? (
                                <Link to={"/word/" + page.wordId}>
                                    {page.word}
                                </Link>
                            ) : (
                                page.word
                            )}
                        </td>
                        <td
                            style={{
                                wordBreak: "normal",
                                wordWrap: "normal",
                            }}
                        >
                            {page.snippet.split(" ").map((s, j) => {
                                Object.keys(patterns).forEach(k => {
                                    s = s.split(k).join(patterns[k]);
                                });
                                const symbol = j === 0 ? "" : " ";
                                const words = word.split(" ");
                                if (
                                    words.some(
                                        w => w.toLowerCase() === s.toLowerCase()
                                    )
                                ) {
                                    return (
                                        <React.Fragment key={j}>
                                            {symbol}
                                            <span
                                                style={{
                                                    fontWeight: "bold",
                                                    display: "inline-block",
                                                }}
                                            >
                                                {s}
                                            </span>
                                        </React.Fragment>
                                    );
                                } else if (
                                    words.some(
                                        w =>
                                            w.toLowerCase() + "," ===
                                            s.toLowerCase()
                                    )
                                ) {
                                    return (
                                        <React.Fragment key={j}>
                                            {symbol}
                                            <span
                                                style={{
                                                    fontWeight: "bold",
                                                    display: "inline-block",
                                                }}
                                            >
                                                {s.slice(0, -1)}
                                            </span>
                                            ,
                                        </React.Fragment>
                                    );
                                } else if (
                                    words.some(
                                        w =>
                                            w.toLowerCase() + ',"' ===
                                            s.toLowerCase()
                                    )
                                ) {
                                    return (
                                        <React.Fragment key={j}>
                                            {symbol}
                                            <span
                                                style={{
                                                    fontWeight: "bold",
                                                    display: "inline-block",
                                                }}
                                            >
                                                {s.slice(0, -1)}
                                            </span>
                                            {',"'}
                                        </React.Fragment>
                                    );
                                } else if (
                                    words.some(
                                        w =>
                                            w.toLowerCase() + "." ===
                                            s.toLowerCase()
                                    )
                                ) {
                                    return (
                                        <React.Fragment key={j}>
                                            {symbol}
                                            <span
                                                style={{
                                                    fontWeight: "bold",
                                                    display: "inline-block",
                                                }}
                                            >
                                                {s.slice(0, -1)}
                                            </span>
                                            .
                                        </React.Fragment>
                                    );
                                } else if (
                                    words.some(
                                        w =>
                                            w.toLowerCase() + ")" ===
                                            s.toLowerCase()
                                    )
                                ) {
                                    return (
                                        <React.Fragment key={j}>
                                            {symbol}
                                            <span
                                                style={{
                                                    fontWeight: "bold",
                                                    display: "inline-block",
                                                }}
                                            >
                                                {s.slice(0, -1)}
                                            </span>
                                            {")"}
                                        </React.Fragment>
                                    );
                                } else if (
                                    words.some(
                                        w =>
                                            w.toLowerCase() + '"' ===
                                            s.toLowerCase()
                                    )
                                ) {
                                    return (
                                        <React.Fragment key={j}>
                                            {symbol}
                                            <span
                                                style={{
                                                    fontWeight: "bold",
                                                    display: "inline-block",
                                                }}
                                            >
                                                {s.slice(0, -1)}
                                            </span>
                                            {'"'}
                                        </React.Fragment>
                                    );
                                } else if (
                                    words.some(
                                        w =>
                                            "(" + w.toLowerCase() ===
                                            s.toLowerCase()
                                    )
                                ) {
                                    return (
                                        <React.Fragment key={j}>
                                            {symbol}
                                            {"("}
                                            <span
                                                style={{
                                                    fontWeight: "bold",
                                                    display: "inline-block",
                                                }}
                                            >
                                                {s.substr(1)}
                                            </span>
                                        </React.Fragment>
                                    );
                                } else if (
                                    words.some(
                                        w =>
                                            '"' + w.toLowerCase() ===
                                            s.toLowerCase()
                                    )
                                ) {
                                    return (
                                        <React.Fragment key={j}>
                                            {symbol}
                                            {'"'}
                                            <span
                                                style={{
                                                    fontWeight: "bold",
                                                    display: "inline-block",
                                                }}
                                            >
                                                {s.substr(1)}
                                            </span>
                                        </React.Fragment>
                                    );
                                } else if (
                                    words.some(
                                        w =>
                                            '""' + w.toLowerCase() ===
                                            s.toLowerCase()
                                    )
                                ) {
                                    return (
                                        <React.Fragment key={j}>
                                            {symbol}
                                            {'""'}
                                            <span
                                                style={{
                                                    fontWeight: "bold",
                                                    display: "inline-block",
                                                }}
                                            >
                                                {s.substr(1)}
                                            </span>
                                        </React.Fragment>
                                    );
                                } else {
                                    return (
                                        <React.Fragment key={j}>
                                            {symbol}
                                            {s}
                                        </React.Fragment>
                                    );
                                }
                            })}
                            <br />
                            <Button
                                size="sm"
                                color="dark"
                                href={
                                    "https://en.wikipedia.org/wiki/" +
                                    page.word.split(" ").join("_")
                                }
                                target="_blank"
                                rel="noopener noreferrer"
                                style={{ marginTop: 7 }}
                            >
                                {`Check the Wikipedia page for ${page.word}`
                                    .split(" ")
                                    .map((w, j) => {
                                        return (
                                            <React.Fragment key={j}>
                                                {j !== 0 && " "}
                                                <span
                                                    style={{
                                                        display: "inline-block",
                                                    }}
                                                >
                                                    {w}
                                                </span>
                                            </React.Fragment>
                                        );
                                    })}
                            </Button>
                        </td>
                    </tr>
                );
            });

    return (
        <React.Fragment>
            {
                <table
                    className="table table-striped"
                    style={{
                        wordBreak: "normal",
                        wordWrap: "normal",
                        marginBottom: 0,
                    }}
                >
                    <thead>
                        <tr>
                            <th>Page Title</th>
                            <th>Snippet</th>
                        </tr>
                    </thead>
                    <tbody>
                        {pageLoaded ? (
                            data.shift()
                        ) : (
                            <tr>
                                <td>Loading...</td>
                                <td></td>
                            </tr>
                        )}
                    </tbody>
                </table>
            }
            {pageLoaded && (
                <React.Fragment>
                    {pages.length > 50 && (
                        <GoogleAd style={{ padding: "10px 0" }} />
                    )}
                    {data.length > 0 && (
                        <table
                            className="table table-striped"
                            style={{ wordBreak: "break-all", marginBottom: 0 }}
                        >
                            <tbody>
                                <tr style={{ display: "none" }}></tr>
                                {data.splice(0, 9)}
                            </tbody>
                        </table>
                    )}
                    {pages.length > 50 && (
                        <GoogleAd style={{ padding: "10px 0" }} />
                    )}
                    {data.length > 0 && (
                        <table
                            className="table table-striped"
                            style={{ wordBreak: "break-all", marginBottom: 0 }}
                        >
                            <tbody>{data.splice(0, 12)}</tbody>
                        </table>
                    )}
                    {pages.length > 50 && (
                        <GoogleAd style={{ padding: "10px 0" }} />
                    )}
                    {data.length > 0 && (
                        <table
                            className="table table-striped"
                            style={{ wordBreak: "break-all", marginBottom: 0 }}
                        >
                            <tbody>{data.splice(0, 12)}</tbody>
                        </table>
                    )}
                    {pages.length > 50 && (
                        <GoogleAd style={{ padding: "10px 0" }} />
                    )}
                    {data.length > 0 && (
                        <table
                            className="table table-striped"
                            style={{ wordBreak: "break-all", marginBottom: 0 }}
                        >
                            <tbody>{data.splice(0, 12)}</tbody>
                        </table>
                    )}
                    {pages.length > 50 && (
                        <GoogleAd style={{ padding: "10px 0" }} />
                    )}
                    {data.length > 0 && (
                        <table
                            className="table table-striped"
                            style={{ wordBreak: "break-all" }}
                        >
                            <tbody>{data}</tbody>
                        </table>
                    )}
                </React.Fragment>
            )}
        </React.Fragment>
    );
}

class RenderOtherTable extends Component {
    constructor(props) {
        super(props);

        this.state = {
            pages: {},
        };
    }

    componentDidMount() {
        this.props.pagesLoaded && this.fetchData();
    }

    componentDidUpdate(previousProps) {
        if (!previousProps.pagesLoaded && this.props.pagesLoaded) {
            this.fetchData();
        }
    }

    fetchData = async () => {
        const url = `api/WikiWalks/getWordsForCategory?category=${encodeURIComponent(
            this.props.c.category
        )}`;
        const response = await fetch(url);
        const pages = await response.json();
        this.setState({ pages });
    };

    render() {
        const { pages } = this.state;
        const { c, wordId } = this.props;
        return (
            <React.Fragment>
                <section style={this.props.sectionStyle}>
                    <h2 id={c.category}>{c.category}</h2>
                    <table
                        className="table table-striped"
                        style={{ wordBreak: "break-all" }}
                    >
                        <thead>
                            <tr>
                                <th style={{ minWidth: 120 }}>Page Title</th>
                                <th>Snippet</th>
                            </tr>
                        </thead>
                        <tbody>
                            {pages.length > 0 ? (
                                pages.map(page => {
                                    const inlineWords = page.word
                                        .split(" ")
                                        .map((w, j) => {
                                            return (
                                                <React.Fragment key={j}>
                                                    {j !== 0 && " "}
                                                    <span
                                                        style={{
                                                            display:
                                                                "inline-block",
                                                        }}
                                                    >
                                                        {w}
                                                    </span>
                                                </React.Fragment>
                                            );
                                        });
                                    return (
                                        <tr key={page.wordId}>
                                            <td style={{ fontWeight: "bold" }}>
                                                {page.wordId !== wordId &&
                                                page.referenceCount > 4 ? (
                                                    <Link
                                                        to={
                                                            "/word/" +
                                                            page.wordId
                                                        }
                                                    >
                                                        {inlineWords}
                                                    </Link>
                                                ) : (
                                                    inlineWords
                                                )}
                                            </td>
                                            <td>
                                                {page.snippet
                                                    .split(" ")
                                                    .map((w, j) => {
                                                        return (
                                                            <React.Fragment
                                                                key={j}
                                                            >
                                                                {j !== 0 && " "}
                                                                <span
                                                                    style={{
                                                                        display:
                                                                            "inline-block",
                                                                    }}
                                                                >
                                                                    {w}
                                                                </span>
                                                            </React.Fragment>
                                                        );
                                                    })}
                                                <br />
                                                <Button
                                                    size="sm"
                                                    color="dark"
                                                    href={
                                                        "https://en.wikipedia.org/wiki/" +
                                                        page.word
                                                            .split(" ")
                                                            .join("_")
                                                    }
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    style={{ marginTop: 7 }}
                                                >
                                                    {`Check the Wikipedia page for ${page.word}`
                                                        .split(" ")
                                                        .map((w, j) => {
                                                            return (
                                                                <React.Fragment
                                                                    key={j}
                                                                >
                                                                    {j !== 0 &&
                                                                        " "}
                                                                    <span
                                                                        style={{
                                                                            display:
                                                                                "inline-block",
                                                                        }}
                                                                    >
                                                                        {w}
                                                                    </span>
                                                                </React.Fragment>
                                                            );
                                                        })}
                                                </Button>
                                            </td>
                                        </tr>
                                    );
                                })
                            ) : (
                                <tr>
                                    <td>Loading...</td>
                                    <td></td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </section>
            </React.Fragment>
        );
    }
}

class ReturnToIndex extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            showReturnToIndex: false,
        };

        window.addEventListener("scroll", this.judge);
    }

    componentDidMount() {
        for (let i = 0; i < 5; i++) {
            setTimeout(() => {
                this.judge();
            }, i * 1000);
        }
    }

    componentWillUnmount() {
        window.removeEventListener("scroll", this.judge);
    }

    judge = () => {
        const { criteriaId } = this.props;
        const elem = document.getElementById(criteriaId);
        if (!elem) return;

        const height = window.innerHeight;

        const offsetY = elem.getBoundingClientRect().top + 700;
        const t_position = offsetY - height;

        if (t_position >= 0) {
            // 上側の時
            this.setState({
                showReturnToIndex: false,
            });
        } else {
            // 下側の時
            this.setState({
                showReturnToIndex: true,
            });
        }
    };

    render() {
        const { showReturnToIndex } = this.state;
        return (
            <div
                style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    position: "fixed",
                    bottom: 0,
                    left: 0,
                    zIndex: showReturnToIndex ? 99999900 : 0,
                    width: window.innerWidth,
                    height: "40px",
                    opacity: showReturnToIndex ? 1.0 : 0,
                    transition: "all 2s ease",
                    fontSize: "large",
                    backgroundColor: "#DDD",
                }}
            >
                <AnchorLink href={`#indexOfVocabLists`}>
                    {"▲ Return to the index ▲"}
                </AnchorLink>
            </div>
        );
    }
}

export default connect(
    state => state.wikiWalks,
    dispatch => bindActionCreators(actionCreators, dispatch)
)(PagesForTheTitles);
