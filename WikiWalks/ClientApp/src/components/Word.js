import React, { Component } from 'react';
import { bindActionCreators } from 'redux';
import { connect } from 'react-redux';
import { Link } from 'react-router-dom';
import { actionCreators } from '../store/WikiWalks';
import Head from './Helmet';
import AnchorLink from 'react-anchor-link-smooth-scroll';
import { Button } from 'reactstrap';

class PagesForTheTitles extends Component {
    componentDidMount() {
        // This method is called when the component is first added to the document
        this.fetchData();
    }

    fetchData() {
        const wordId = this.props.match.params.wordId;
        this.props.requestPagesForTheTitle(wordId);
    }

    componentDidUpdate(previousProps) {
        const pagesLoaded = previousProps.pages.length <= 0 && this.props.pages[0] && this.props.pages[0].publishDate;
        const changedTheme = previousProps.location !== this.props.location;
        if (pagesLoaded || changedTheme) {
            this.fetchData();
        }
    }

    render() {
        const { wordId, categories } = this.props.pages;

        if (("" + wordId) !== this.props.match.params.wordId) return <p>Loading...</p>;

        const word = this.props.pages.word || "";
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
                {<div className="breadcrumbs" itemScope itemType="https://schema.org/BreadcrumbList" style={{ textAlign: "left" }}>
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
                </div>}
                <hr />
                <h1>{word}</h1>
                <br />
                {lineChangeDesc}
                <br />
                {
                    categories && categories.length > 0 &&
                    <div style={{ maxWidth: "500px", padding: "10px", marginBottom: "10px", border: "5px double gray" }}>
                        <center><p style={{ fontWeight: "bold", fontSize: "large" }}>Index</p></center>
                        <hr />
                        {word ? <ul>
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
                <hr />
                <h2 id={`Pages about ${word}`}>{`Pages about ${word}`}</h2>
                {renderTable(this.props)}
                {categories && categories.length > 0 && categories.map((c, i) => (
                    <RenderOtherTable
                        key={i}
                        c={c}
                        wordId={wordId}
                    />
                ))}
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
            <hr />
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
                                    {page.snippet}
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
        </React.Fragment>);
    }
}

export default connect(
    state => state.wikiWalks,
    dispatch => bindActionCreators(actionCreators, dispatch)
)(PagesForTheTitles);
