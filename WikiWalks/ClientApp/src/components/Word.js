import React, { Component } from 'react';
import { bindActionCreators } from 'redux';
import { connect } from 'react-redux';
import { Link } from 'react-router-dom';
import AnchorLink from 'react-anchor-link-smooth-scroll';
import { Button } from 'reactstrap';
import { actionCreators } from '../store/WikiWalks';
import Head from './Helmet';
import GoogleAd from './GoogleAd';

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
    }

    componentDidMount() {
        this.fetchData();
    }

    fetchData() {
        this.props.initialize();
        const wordId = this.props.match.params.wordId.split("#")[0];
        this.props.requestWord(wordId);
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

        const { wordId, categories, pages } = this.props.pages;
        const word = this.props.word || "Loading...";
        const cat = categories && categories.sort((c1, c2) => c2.cnt - c1.cnt)[0];
        const category = cat && cat.category;
        const categoryForUrl = category && encodeURIComponent(category.split(" ").join("_"));
        const description = `This is a list of the Wikipedia pages about "${word}". Please check the list below to learn about "${word}"!`;
        const arrDesc = description.split(". ");
        const lineChangeDesc = arrDesc.map((d, i) => <span key={i}>{d}{i < arrDesc.length - 1 && ". "}<br /></span>);

        return (
            <div>
                <Head
                    title={word}
                    desc={description}
                />
                {
                    pages && pages.length > 50 &&
                    <GoogleAd
                        style={{ marginBottom: 20 }}
                    />
                }
                <div className="breadcrumbs" itemScope itemType="https://schema.org/BreadcrumbList" style={{ textAlign: "left" }}>
                    <span itemProp="itemListElement" itemScope itemType="http://schema.org/ListItem">
                        <Link to="/" itemProp="item" style={{ marginRight: "5px", marginLeft: "5px" }}>
                            <span itemProp="name">
                                {"Home"}
                            </span>
                        </Link>
                        <meta itemProp="position" content="1" />
                    </span>
                    {" > "}
                    {
                        category ?
                            <span itemProp="itemListElement" itemScope itemType="http://schema.org/ListItem">
                                <Link to={"/category/" + categoryForUrl} itemProp="item" style={{ marginRight: "5px", marginLeft: "5px" }}>
                                    <span itemProp="name">
                                        {category}
                                    </span>
                                    <meta itemProp="position" content="2" />
                                </Link>
                            </span>
                            :
                            word && <span itemProp="itemListElement" itemScope itemType="http://schema.org/ListItem">
                                <Link to={"/all"} itemProp="item" style={{ marginRight: "5px", marginLeft: "5px" }}>
                                    <span itemProp="name">
                                        {"All Keywords"}
                                    </span>
                                    <meta itemProp="position" content="2" />
                                </Link>
                            </span>
                    }
                    {" > "}
                    <span itemProp="itemListElement" itemScope itemType="http://schema.org/ListItem">
                        <span itemProp="name" style={{ marginRight: "5px", marginLeft: "5px" }}>
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
                        {
                            categories && categories.length > 0 &&
                            <div style={{ maxWidth: 500, padding: 10, marginBottom: isWide ? 20 : 30, marginRight: isWide ? 20 : 0, border: "5px double gray", width: "100%" }}>
                                <center><p style={{ fontWeight: "bold", fontSize: "large" }}>Index</p></center>
                                {word ? <ul style={{ ...this.sectionStyle, marginBottom: 0, }}>
                                    <li><AnchorLink href={`#Pages about ${word}`}>{`Pages about ${word}`}</AnchorLink></li>
                                    {categories.map((c, i) => (
                                        <li key={i}><AnchorLink href={"#" + c.category}>{c.category}</AnchorLink></li>
                                    ))}
                                </ul>
                                    :
                                    <center>Loading...<br /></center>
                                }
                            </div>
                        }
                        {
                            pages && pages.length > 50 &&
                            <GoogleAd
                                style={{ width: "100%", marginBottom: isWide ? 0 : 30 }}
                            />
                        }
                    </div>
                    <section style={this.sectionStyle}>
                        <h2 id={`Pages about ${word}`}>{`Pages about ${word}`}</h2>
                        {renderTable(this.props)}
                    </section>
                    {
                        pages && pages.length > 50 && <GoogleAd />
                    }
                    {categories && categories.length > 0 && categories.map((c, i) => (
                        <RenderOtherTable
                            key={i}
                            c={c}
                            wordId={wordId}
                            sectionStyle={this.sectionStyle}
                        />
                    ))}
                    {
                        categories && categories.length > 0 &&
                        <React.Fragment>
                            <ReturnToIndex
                                refForReturnToIndex={this.refForReturnToIndex}
                                criteriaId={`Pages about ${word}`}
                            />
                            <div style={{ height: "50px" }}></div>
                        </React.Fragment>
                    }
                </article>
            </div>
        );
    }
}

function renderTable(props) {
    const { pages, wordId, word } = props.pages;
    return (
        <table className='table table-striped' style={{ wordBreak: "break-all" }}>
            <thead>
                <tr>
                    <th style={{ minWidth: 120 }}>Page Title</th>
                    <th>Snippet</th>
                </tr>
            </thead>
            <tbody>
                {pages && pages.length > 0 ?
                    pages
                        .sort((page1, page2) => page2.snippet.split(word).length - page1.snippet.split(word).length)
                        .sort((page1, page2) => {
                            if (page2.wordId === wordId) {
                                return 1;
                            } else if (page1.wordId === wordId) {
                                return -1;
                            } else {
                                return 0;
                            }
                        })
                        .map((page, i) => {
                            const inlineWords = page.word.split(" ").map((w, j) => {
                                return <React.Fragment key={j}>{j !== 0 && " "}<span style={{ display: "inline-block" }}>{w}</span></React.Fragment>;
                            });
                            return (
                                <tr key={i}>
                                    <td style={{ fontWeight: "bold" }}>
                                        {page.wordId !== wordId && page.referenceCount > 4 ? <Link to={"/word/" + page.wordId}>{inlineWords}</Link> : inlineWords}
                                    </td>
                                    <td>
                                        {page.snippet.split(" ").map((s, j) => {
                                            const patterns = {
                                                '&lt;': '<',
                                                '&gt;': '>',
                                                '&amp;': '&',
                                                '&quot;': '"',
                                                '&#x27;': '\'',
                                                '&#x60;': '`'
                                            };
                                            Object.keys(patterns).forEach(k => { s = s.split(k).join(patterns[k]) });
                                            const symbol = j === 0 ? "" : " ";
                                            const words = props.pages.word.split(" ");
                                            if (words.some(w => w.toLowerCase() === s.toLowerCase())) {
                                                return <React.Fragment key={j}>{symbol}<span style={{ fontWeight: "bold", display: "inline-block" }}>{s}</span></React.Fragment>;
                                            } else if (words.some(w => (w.toLowerCase() + ",") === s.toLowerCase())) {
                                                return <React.Fragment key={j}>{symbol}<span style={{ fontWeight: "bold", display: "inline-block" }}>{s.slice(0, -1)}</span>,</React.Fragment>;
                                            } else if (words.some(w => (w.toLowerCase() + ",\"") === s.toLowerCase())) {
                                                return <React.Fragment key={j}>{symbol}<span style={{ fontWeight: "bold", display: "inline-block" }}>{s.slice(0, -1)}</span>{",\""}</React.Fragment>;
                                            } else if (words.some(w => (w.toLowerCase() + ".") === s.toLowerCase())) {
                                                return <React.Fragment key={j}>{symbol}<span style={{ fontWeight: "bold", display: "inline-block" }}>{s.slice(0, -1)}</span>.</React.Fragment>;
                                            } else if (words.some(w => (w.toLowerCase() + ")") === s.toLowerCase())) {
                                                return <React.Fragment key={j}>{symbol}<span style={{ fontWeight: "bold", display: "inline-block" }}>{s.slice(0, -1)}</span>{")"}</React.Fragment>;
                                            } else if (words.some(w => (w.toLowerCase() + "\"") === s.toLowerCase())) {
                                                return <React.Fragment key={j}>{symbol}<span style={{ fontWeight: "bold", display: "inline-block" }}>{s.slice(0, -1)}</span>{"\""}</React.Fragment>;
                                            } else if (words.some(w => ("(" + w.toLowerCase()) === s.toLowerCase())) {
                                                return <React.Fragment key={j}>{symbol}{"("}<span style={{ fontWeight: "bold", display: "inline-block" }}>{s.substr(1)}</span></React.Fragment>;
                                            } else if (words.some(w => ("\"" + w.toLowerCase()) === s.toLowerCase())) {
                                                return <React.Fragment key={j}>{symbol}{"\""}<span style={{ fontWeight: "bold", display: "inline-block" }}>{s.substr(1)}</span></React.Fragment>;
                                            } else if (words.some(w => ("\"\"" + w.toLowerCase()) === s.toLowerCase())) {
                                                return <React.Fragment key={j}>{symbol}{"\"\""}<span style={{ fontWeight: "bold", display: "inline-block" }}>{s.substr(1)}</span></React.Fragment>;
                                            } else {
                                                return <React.Fragment key={j}>{symbol}<span style={{ display: "inline-block" }}>{s}</span></React.Fragment>;
                                            }
                                        })}
                                        <br />
                                        <Button
                                            size="sm"
                                            color="dark"
                                            href={"https://en.wikipedia.org/wiki/" + page.word.split(" ").join("_")}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            style={{ marginTop: 7 }}
                                        >
                                            {`Check the Wikipedia page for ${page.word}`.split(" ").map((w, j) => {
                                                return <React.Fragment key={j}>{j !== 0 && " "}<span style={{ display: "inline-block" }}>{w}</span></React.Fragment>;
                                            })}
                                        </Button>
                                    </td>
                                </tr>
                            );
                        })
                    :
                    <tr><td>Loading...</td><td></td></tr>}
            </tbody>
        </table>
    );
}

class RenderOtherTable extends Component {

    constructor(props) {
        super(props);

        this.state = {
            pages: {},
        }
    }

    componentDidMount() {
        this.fetchData();
    }

    componentDidUpdate(previousProps) {
        if (previousProps.c !== this.props.c) {
            this.fetchData();
        }
    }

    fetchData = async () => {
        const url = `api/WikiWalks/getWordsForCategory?category=${encodeURIComponent(this.props.c.category)}`;
        const response = await fetch(url);
        const pages = await response.json();
        this.setState({ pages });
    }

    render() {
        const { pages } = this.state;
        const { c, wordId } = this.props;
        return (<React.Fragment>
            <section style={this.props.sectionStyle}>
                <h2 id={c.category}>{c.category}</h2>
                <table className='table table-striped' style={{ wordBreak: "break-all" }}>
                    <thead>
                        <tr>
                            <th style={{ minWidth: 120 }}>Page Title</th>
                            <th>Snippet</th>
                        </tr>
                    </thead>
                    <tbody>
                        {pages.length > 0 ? pages.map(page => {
                            const inlineWords = page.word.split(" ").map((w, j) => {
                                return <React.Fragment key={j}>{j !== 0 && " "}<span style={{ display: "inline-block" }}>{w}</span></React.Fragment>;
                            });
                            return (
                                <tr key={page.wordId}>
                                    <td style={{ fontWeight: "bold" }}>
                                        {(page.wordId !== wordId && page.referenceCount > 4) ? <Link to={"/word/" + page.wordId}>{inlineWords}</Link> : inlineWords}
                                    </td>
                                    <td>
                                        {page.snippet.split(" ").map((w, j) => {
                                            return <React.Fragment key={j}>{j !== 0 && " "}<span style={{ display: "inline-block" }}>{w}</span></React.Fragment>;
                                        })}
                                        <br />
                                        <Button
                                            size="sm"
                                            color="dark"
                                            href={"https://en.wikipedia.org/wiki/" + page.word.split(" ").join("_")}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            style={{ marginTop: 7 }}
                                        >
                                            {`Check the Wikipedia page for ${page.word}`.split(" ").map((w, j) => {
                                                return <React.Fragment key={j}>{j !== 0 && " "}<span style={{ display: "inline-block" }}>{w}</span></React.Fragment>;
                                            })}
                                        </Button>
                                    </td>
                                </tr>
                            );
                        })
                            :
                            <tr><td>Loading...</td><td></td></tr>}
                    </tbody>
                </table>
            </section>
        </React.Fragment>);
    }
}

class ReturnToIndex extends React.Component {

    constructor(props) {
        super(props);
        this.state = {
            showReturnToIndex: false,
        }

        window.addEventListener('scroll', this.judge);
    }

    componentDidMount() {
        for (let i = 0; i < 5; i++) {
            setTimeout(() => {
                this.judge();
            }, i * 1000);
        }
    }

    componentWillUnmount() {
        window.removeEventListener('scroll', this.judge);
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
    }

    render() {
        const { showReturnToIndex } = this.state;
        return (
            <div style={{
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
            }}>
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
