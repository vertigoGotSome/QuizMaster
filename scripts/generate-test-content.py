import json
from pathlib import Path

out = Path('samples/generated')

words = [
    ('Alltag','Reifen','rund'),('Alltag','Toilettensitz','sauber'),('Tiere','Igel','leise'),
    ('Tiere','Pinguin','elegant'),('Tiere','Kamel','Ausdauer'),('Tiere','Oktopus','neugierig'),
    ('Essen','Pizza','geteilt'),('Essen','Sushi','klein'),('Essen','Kaffee','morgens'),
    ('Essen','Popcorn','Film'),('Technik','Kopfhörer','privat'),('Technik','Tastatur','schnell'),
    ('Technik','Fernbedienung','bequem'),('Schule','Radiergummi','Fehler'),('Schule','Lineal','gerade'),
    ('Orte','Bahnhof','warten'),('Orte','Flughafen','Abschied'),('Orte','Schwimmbad','Sommer'),
    ('Sport','Fußball','Team'),('Sport','Fahrrad','Balance'),('Natur','Regenbogen','kurz'),
    ('Natur','Vulkan','Ruhe'),('Kultur','Kino','dunkel'),('Kultur','Museum','still'),
    ('Gaming','Controller','Daumen'),('Gaming','Minecraft','kreativ'),('Reisen','Koffer','vorbereitet'),
    ('Reisen','Landkarte','Richtung'),('Musik','Mikrofon','mutig'),('Musik','Gitarre','Saiten'),
]
json.dump([{'category': c, 'word': w, 'hint': h} for c,w,h in words], open(out/'imposter-30-subtile-hilfswoerter.json','w',encoding='utf8'), ensure_ascii=False, indent=2)

# Two questions for every level permit two complete 30-question WWM games.
def q(category, difficulty, text, answers, correct):
    return {'category':category,'difficulty':difficulty,'text':text,
            'answerA':answers[0],'answerB':answers[1],'answerC':answers[2],'answerD':answers[3],'correct':correct}
wwm = [
q('Sprichwörter',1,'Wie endet das Sprichwort: „Ende gut, ...“?',['alles gut','Ende besser','Glück gehabt','weiter so'],'A'),
q('Alltag',1,'Welche Farbe entsteht aus Blau und Gelb?',['Rot','Grün','Lila','Orange'],'B'),
q('Sprichwörter',2,'Wer anderen eine Grube gräbt, ...',['fällt selbst hinein','gewinnt immer','baut ein Haus','hat Glück'],'A'),
q('Alltag',2,'Wie viele Tage hat ein Wochenende?',['1','2','3','4'],'B'),
q('Tiere',3,'Welches Tier legt typischerweise Eier?',['Katze','Hund','Huhn','Pferd'],'C'),
q('Geografie',3,'In welchem Land liegt Berlin?',['Österreich','Deutschland','Schweiz','Belgien'],'B'),
q('Sprache',4,'Welches Wort ist ein Synonym für „fröhlich“?',['heiter','müde','leise','streng'],'A'),
q('Alltag',4,'Womit misst man gewöhnlich die Zeit?',['Thermometer','Uhr','Lineal','Waage'],'B'),
q('Natur',5,'Welche Jahreszeit folgt auf den Frühling?',['Herbst','Winter','Sommer','Montag'],'C'),
q('Essen',5,'Welches davon ist eine Frucht?',['Kartoffel','Apfel','Brot','Käse'],'B'),
q('Tiere',6,'Wie nennt man ein männliches Pferd?',['Hengst','Bulle','Widder','Fohlen'],'A'),
q('Sport',6,'Wie viele Spieler stehen in einem Fußballteam normalerweise gleichzeitig auf dem Feld?',['7','9','11','13'],'C'),
q('Kultur',7,'Wer malte die Mona Lisa?',['Vincent van Gogh','Leonardo da Vinci','Pablo Picasso','Claude Monet'],'B'),
q('Natur',7,'Welcher Planet ist als roter Planet bekannt?',['Venus','Mars','Jupiter','Neptun'],'B'),
q('Deutschland',8,'Welcher Fluss fließt durch Köln?',['Elbe','Donau','Rhein','Oder'],'C'),
q('Musik',8,'Wie viele Saiten hat eine klassische Gitarre üblicherweise?',['4','5','6','8'],'C'),
q('Wissenschaft',9,'Welches Gas atmen Menschen zum Überleben ein?',['Sauerstoff','Helium','Wasserstoff','Kohlendioxid'],'A'),
q('Film',9,'Wie heißt die Schule in Harry Potter?',['Narnia','Hogwarts','Mittelerde','Westeros'],'B'),
q('Geschichte',10,'In welchem Jahr fiel die Berliner Mauer?',['1979','1989','1999','2009'],'B'),
q('Geografie',10,'Welche Hauptstadt gehört zu Kanada?',['Toronto','Vancouver','Ottawa','Montreal'],'C'),
q('Wissenschaft',11,'Welches Element trägt das Symbol Au?',['Silber','Gold','Argon','Aluminium'],'B'),
q('Literatur',11,'Wer schrieb „Romeo und Julia“?',['Goethe','Shakespeare','Kafka','Schiller'],'B'),
q('Sport',12,'In welcher Sportart gibt es einen Grand Slam in Wimbledon?',['Golf','Tennis','Boxen','Handball'],'B'),
q('Politik',12,'Wie heißt das deutsche Parlament?',['Bundestag','Landtag','Reichstag','Stadtrat'],'A'),
q('Natur',13,'Wie heißt die größte Wüste der Erde?',['Sahara','Gobi','Antarktis','Kalahari'],'C'),
q('Technik',13,'Wofür steht die Abkürzung CPU?',['Computer Processing Unit','Central Processing Unit','Control Power Unit','Central Program Utility'],'B'),
q('Geschichte',14,'Welche antike Stadt wurde 79 n. Chr. vom Vesuv verschüttet?',['Troja','Pompeji','Sparta','Alexandria'],'B'),
q('Kunst',14,'Welche Kunstrichtung prägte Salvador Dalí?',['Kubismus','Surrealismus','Impressionismus','Barock'],'B'),
q('Biologie',15,'Welcher Teil einer Zelle enthält überwiegend die Erbinformation?',['Zellkern','Zellwand','Vakuole','Ribosom'],'A'),
q('Musik',15,'Wer wird oft „King of Pop“ genannt?',['Elvis Presley','Michael Jackson','Prince','David Bowie'],'B'),
q('Geografie',16,'Welcher Staat hat die längste Küstenlinie der Welt?',['Australien','Kanada','Indonesien','Russland'],'B'),
q('Wissenschaft',16,'Wie lautet die chemische Formel von Kochsalz?',['NaCl','H2O','CO2','O2'],'A'),
q('Geschichte',17,'Welcher Vertrag beendete 1919 formal den Ersten Weltkrieg?',['Vertrag von Versailles','Münchner Abkommen','Potsdamer Abkommen','NATO-Vertrag'],'A'),
q('Literatur',17,'Von wem stammt der Roman „Der Prozess“?',['Thomas Mann','Franz Kafka','Bertolt Brecht','Hermann Hesse'],'B'),
q('Astronomie',18,'Wie heißt der größte Mond des Saturn?',['Europa','Titan','Io','Triton'],'B'),
q('Wirtschaft',18,'Welche Institution gibt in Deutschland Banknoten aus?',['Bundesbank im Eurosystem','Finanzamt','Bundestag','Sparkasse'],'A'),
q('Biologie',19,'Welche Blutgruppe gilt als Universalspender für rote Blutkörperchen?',['AB positiv','A negativ','0 negativ','B positiv'],'C'),
q('Kunst',19,'Welcher Künstler schuf „Die Sternennacht“?',['Claude Monet','Vincent van Gogh','Edvard Munch','Paul Cézanne'],'B'),
q('Physik',20,'Welche Einheit misst elektrische Spannung?',['Watt','Volt','Ohm','Ampere'],'B'),
q('Geschichte',20,'Welcher Pharao ist mit einem nahezu unberührten Grabfund verbunden?',['Tutanchamun','Ramses II.','Cheops','Echnaton'],'A'),
q('Chemie',21,'Welche Ordnungszahl hat Kohlenstoff?',['4','6','8','12'],'B'),
q('Geografie',21,'Durch welche Meerenge ist Europa von Afrika bei Spanien getrennt?',['Beringstraße','Straße von Gibraltar','Bosporus','Dardanellen'],'B'),
q('Physik',22,'Wie heißt die SI-Einheit der elektrischen Kapazität?',['Farad','Tesla','Joule','Pascal'],'A'),
q('Literatur',22,'Welche Figur spricht „Sein oder Nichtsein“?',['Faust','Hamlet','Othello','Macbeth'],'B'),
q('Biologie',23,'Welches Organ produziert Insulin?',['Leber','Bauchspeicheldrüse','Niere','Milz'],'B'),
q('Geschichte',23,'Welche Revolution begann 1789 in Frankreich?',['Industrielle Revolution','Französische Revolution','Glorreiche Revolution','Februarrevolution'],'B'),
q('Mathematik',24,'Wie viele Seiten besitzt ein Ikosaeder?',['12','16','20','24'],'C'),
q('Astronomie',24,'Welcher Stern ist der Sonne am nächsten?',['Sirius','Proxima Centauri','Wega','Beteigeuze'],'B'),
q('Informatik',25,'Wofür steht SQL?',['Structured Query Language','Simple Question List','System Queue Link','Standard Quality Logic'],'A'),
q('Musik',25,'Welcher Komponist schrieb die „Mondscheinsonate“?',['Mozart','Beethoven','Bach','Chopin'],'B'),
q('Physik',26,'Wie heißt der Effekt, bei dem Licht durch ein Prisma in Farben zerlegt wird?',['Reflexion','Dispersion','Induktion','Diffusion'],'B'),
q('Chemie',26,'Welches Element ist bei Raumtemperatur flüssig?',['Brom','Eisen','Natrium','Schwefel'],'A'),
q('Geografie',27,'Welcher Fluss mündet nicht ins Mittelmeer?',['Nil','Ebro','Donau','Rhone'],'C'),
q('Geschichte',27,'Welcher Herrscher ließ den Petersdom neu errichten?',['Julius II.','Ludwig XIV.','Karl der Große','Napoleon'],'A'),
q('Biologie',28,'Welche Basenpaarung gehört zur DNA?',['Adenin–Thymin','Adenin–Uracil','Guanin–Uracil','Cytosin–Uracil'],'A'),
q('Mathematik',28,'Wie lautet die Ableitung von x²?',['x','2x','x²','2'],'B'),
q('Physik',29,'Welche Größe bleibt bei einem ideal elastischen Stoß zusätzlich zum Impuls erhalten?',['Temperatur','kinetische Energie','Massezahl','Ladung'],'B'),
q('Philosophie',29,'Welcher Philosoph schrieb die „Kritik der reinen Vernunft“?',['Kant','Hegel','Nietzsche','Descartes'],'A'),
q('Astronomie',30,'Wie heißt die Grenze um ein Schwarzes Loch, ab der nichts mehr entkommt?',['Ereignishorizont','Asteroidengürtel','Koronaloch','Roche-Grenze'],'A'),
q('Geschichte',30,'Welches Schriftstück beginnt mit „We the People“?',['Magna Carta','US-Verfassung','Bill of Rights','Unabhängigkeitserklärung'],'B'),
]
assert len(wwm) == 60
json.dump(wwm, open(out/'wwm-60-zwei-runden.json','w',encoding='utf8'), ensure_ascii=False, indent=2)

# Each category has five accessible first-board cells and five harder second-board cells.
# Flag questions use stable external PNG links that can be replaced in the Jeopardy editor later.
C = {}
def add(category, r1, r2): C[category]=(r1,r2)
add('Geographie',[
('Welche Hauptstadt gehört zu Italien?','Rom'),('Welcher Kontinent ist der größte?','Asien'),('Durch welches Land fließt der Nil hauptsächlich?','Ägypten'),('Wie heißt der höchste Berg der Erde?','Mount Everest'),('Welche Stadt wird „Big Apple“ genannt?','New York')],[
('Welche Hauptstadt liegt am Fluss Tiber?','Rom'),('Welches Land grenzt sowohl an Portugal als auch an Frankreich?','Spanien'),('Wie heißt die Meerenge zwischen Alaska und Russland?','Beringstraße'),('Welcher Staat hat keine Hauptstadt an der Küste?','Schweiz'),('Welcher Fluss fließt durch Wien, Bratislava, Budapest und Belgrad?','Donau')])
add('Fußball',[
('Wie viele Spieler hat ein Team auf dem Platz?','11'),('Wie heißt ein Treffer direkt aus einer Ecke?','Olympisches Tor'),('Welche Karte bedeutet Platzverweis?','Rot'),('Wie lang dauert ein reguläres Fußballspiel ohne Nachspielzeit?','90 Minuten'),('Wie heißt der wichtigste Wettbewerb für Nationalteams in Europa?','Europameisterschaft')],[
('Welches Land gewann die Fußball-WM 2014?','Deutschland'),('Wie heißt der Torwartbereich direkt vor dem Tor?','Fünfmeterraum'),('Welcher Spieler gewann die meisten Ballon-d’Or-Auszeichnungen?','Lionel Messi'),('Wie nennt man drei Tore eines Spielers in einem Spiel?','Hattrick'),('Welche Regel verhindert, dass ein Angreifer beim Abspiel zu weit vorn steht?','Abseitsregel')])
add('Pizza',[
('Welche Zutat gehört klassisch auf eine Margherita?','Mozzarella'),('Aus welchem Land stammt Pizza?','Italien'),('Welche Form hat eine klassische Pizza meistens?','Rund'),('Wie heißt Pizza mit Tomate, Mozzarella und Basilikum?','Margherita'),('Was kommt typischerweise unter den Belag?','Tomatensauce')],[
('Welche Stadt gilt als Geburtsort der modernen Pizza?','Neapel'),('Welche Käseart wird für Pizza Quattro Formaggi nicht zwingend verwendet?','Antwort abhängig von Rezept'),('Wofür stehen die Farben einer Margherita symbolisch?','Italienische Flagge'),('Wie heißt die gefaltete Pizza-Variante aus Italien?','Calzone'),('Was bedeutet „al taglio“ bei Pizza?','Stückweise geschnitten')])
add('Wüstenmaus',[
('Wie viele Beine hat eine Wüstenmaus?','4'),('Zu welcher Tiergruppe gehört eine Wüstenmaus?','Säugetier'),('Womit gräbt eine Wüstenmaus häufig?','Mit den Pfoten'),('Wann sind viele Wüstenmäuse besonders aktiv?','Nachts'),('Was hilft ihr in trockenen Gebieten?','Sie braucht wenig Wasser')],[
('Wie heißt die Familie, zu der viele Wüstenmäuse gehören?','Mäuseverwandte'),('Warum sind große Ohren in der Wüste praktisch?','Wärmeabgabe und gutes Hören'),('Wie schützt sich eine Wüstenmaus vor Hitze?','Sie bleibt tagsüber im Bau'),('Welche Farbe hat ihr Fell oft?','Sandfarben'),('Welche Gefahr erkennt sie mit gutem Gehör früh?','Raubtiere')])
add('Wetter',[
('Wie nennt man gefrorenes Wasser, das vom Himmel fällt?','Schnee'),('Welches Gerät misst die Temperatur?','Thermometer'),('Was zeigt ein Regenbogen nach Regen und Sonne?','Farben im Licht'),('Wie heißt bewegte Luft?','Wind'),('Welche Wolken bringen oft Gewitter?','Cumulonimbus')],[
('Welche Einheit wird beim Luftdruck häufig verwendet?','Hektopascal'),('Wie heißt der Übergang von Wasser zu Wasserdampf?','Verdunstung'),('Was misst ein Hygrometer?','Luftfeuchtigkeit'),('Wie heißt ein sehr starker Wirbelsturm über warmem Meer?','Hurrikan'),('Warum entsteht Blitz vor dem Donner wahrnehmbar?','Licht ist schneller als Schall')])
add('Flaggen',[
('Welche Flagge zeigt Schwarz, Rot und Gold?','Deutschland','https://flagcdn.com/w320/de.png'),('Welche Flagge zeigt einen roten Kreis auf weißem Grund?','Japan','https://flagcdn.com/w320/jp.png'),('Welche Flagge hat Ahornblatt in der Mitte?','Kanada','https://flagcdn.com/w320/ca.png'),('Welche Flagge zeigt grün-weiß-rot senkrecht?','Italien','https://flagcdn.com/w320/it.png'),('Welche Flagge zeigt Sterne und Streifen?','USA','https://flagcdn.com/w320/us.png')],[
('Welche Flagge zeigt ein rotes Kreuz auf weißem Grund?','Schweiz','https://flagcdn.com/w320/ch.png'),('Welche Flagge zeigt ein gelbes Kreuz auf blauem Grund?','Schweden','https://flagcdn.com/w320/se.png'),('Welche Flagge zeigt eine aufgehende Sonne mit Strahlen?','Japan','https://flagcdn.com/w320/jp.png'),('Welche Flagge zeigt ein grünes Zedernblatt?','Libanon','https://flagcdn.com/w320/lb.png'),('Welche Flagge hat Blau, Gelb und Rot als senkrechte Streifen?','Rumänien','https://flagcdn.com/w320/ro.png')])
add('Erfindungen',[
('Wer wird oft mit dem praktischen Glühlampenmodell verbunden?','Thomas Edison'),('Womit telefoniert man drahtlos unterwegs?','Smartphone'),('Was druckt ein Drucker auf Papier?','Text oder Bilder'),('Welche Erfindung transportiert Menschen in oberen Stockwerke?','Aufzug'),('Womit wird ein Foto digital aufgenommen?','Kamera')],[
('Wer erfand den Buchdruck mit beweglichen Lettern in Europa?','Johannes Gutenberg'),('Welche Erfindung ermöglichte weltweite Computernetzwerke?','Internet'),('Wer entwickelte die erste brauchbare Dampfmaschine entscheidend weiter?','James Watt'),('Wie heißt die Erfindung der Brüder Wright?','Flugzeug'),('Welche Technologie speichert Daten auf einem Chip statt einer Magnetplatte?','SSD')])
add('Fast Energy',[
('Welcher Stoff steckt oft in Energy Drinks und macht wach?','Koffein'),('Welche Farbe wird häufig mit dem Geschmack „Original“ verbunden?','Antwort abhängig von Marke'),('Was sollte man bei Energy Drinks besonders beachten?','Koffeingehalt'),('Womit sollte man Energy Drinks nicht gedankenlos mischen?','Alkohol'),('Welche Alternative hilft ebenfalls bei Müdigkeit?','Schlaf')],[
('Welcher Inhaltsstoff wird häufig als Aminosäure in Energy Drinks beworben?','Taurin'),('Warum ist viel Zucker in Energy Drinks ein Thema?','Hoher Kaloriengehalt'),('Welche Gruppe sollte Energy Drinks besonders vorsichtig konsumieren?','Kinder und Jugendliche'),('Was ist die übliche Mengeneinheit auf der Dose für Koffein?','Milligramm'),('Was passiert bei zu viel Koffein oft?','Nervosität oder Herzklopfen')])
add('Game of the Year',[
('Wie nennt man die Auszeichnung für das beste Videospiel eines Jahres oft?','Game of the Year'),('Welche Plattform ist keine Spielkonsole?','Toaster'),('Wie heißt die Figur mit grünem Hut aus Nintendo-Spielen?','Link'),('In welchem Spiel baut man Blöcke und überlebt nachts?','Minecraft'),('Wie heißt der blaue Igel von Sega?','Sonic')],[
('Welches Spiel gewann bei den Game Awards 2023 „Game of the Year“?','Baldur’s Gate 3'),('Welches Spiel gewann bei den Game Awards 2022 „Game of the Year“?','Elden Ring'),('Wie heißt die Spielereihe mit Geralt von Riva?','The Witcher'),('Welches Studio entwickelte Hades?','Supergiant Games'),('Wie heißt die Engine hinter vielen Spielen von Epic?','Unreal Engine')])
add('Musik',[
('Wie viele Tasten hat ein Klavier üblicherweise?','88'),('Welches Instrument hat Saiten und sechs häufig?','Gitarre'),('Wer wird „King of Pop“ genannt?','Michael Jackson'),('Wie heißt der lauteste Teil eines Popsongs oft?','Refrain'),('Welches Instrument spielt man mit Stöcken?','Schlagzeug')],[
('Wie heißt die Tonart ohne Vorzeichen, die oft als Grundtonart genannt wird?','C-Dur'),('Wer komponierte die 9. Sinfonie mit „Ode an die Freude“?','Ludwig van Beethoven'),('Wie nennt man ein Musikstück für ein einzelnes Instrument?','Solo'),('Was beschreibt das Tempo in der Musik?','Geschwindigkeit'),('Wie heißt ein Lied ohne Instrumentalbegleitung?','A cappella')])
add('Dinosaurier',[
('Welcher Dinosaurier hatte drei Hörner im Gesicht?','Triceratops'),('Welcher Dinosaurier ist für seine kurzen Arme bekannt?','Tyrannosaurus rex'),('Was fraßen Pflanzenfresser hauptsächlich?','Pflanzen'),('Wie nennt man versteinerte Überreste?','Fossilien'),('Lebten Dinosaurier vor oder nach Menschen?','Vor Menschen')],[
('In welcher Erdzeit lebte der T. rex?','Kreidezeit'),('Wie heißt der Dinosaurier mit langem Hals?','Brachiosaurus'),('Was endete vor etwa 66 Millionen Jahren mit einem Massenaussterben?','Nicht-vogelartige Dinosaurier'),('Wie nennt man einen Dinosaurier, der Fleisch frisst?','Karnivore'),('Welche heutigen Tiere gelten als direkte Dinosaurier-Nachfahren?','Vögel')])
add('Schule',[
('Wie viele Minuten hat eine Stunde?','60'),('Wie heißt das Ergebnis einer Addition?','Summe'),('Welche Farbe hat Chlorophyll?','Grün'),('Wie viele Seiten hat ein Dreieck?','3'),('Wer schrieb „Faust“?','Goethe')],[
('Wie heißt die Formel für die Fläche eines Rechtecks?','Länge mal Breite'),('Welche Wortart beschreibt ein Nomen näher?','Adjektiv'),('Wie heißt der Prozess, bei dem Pflanzen Licht in Energie umwandeln?','Photosynthese'),('Was ist die Hauptstadt von Australien?','Canberra'),('Wie nennt man den Satz des Pythagoras kurz?','a² plus b² gleich c²')])
add('Film & Serien',[
('Wie heißt der Zauberer mit Narbe auf der Stirn?','Harry Potter'),('Wie heißt die Eiskönigin aus Disney?','Elsa'),('Welche Farbe hat Shrek?','Grün'),('Wie heißt der Cowboy aus Toy Story?','Woody'),('Wie heißt die Stadt von Batman?','Gotham City')],[
('Wer führte bei „Titanic“ Regie?','James Cameron'),('Wie heißt die Serie mit dem Satz „Winter is coming“?','Game of Thrones'),('Welche Figur sagt „Ich bin dein Vater“ in Star Wars?','Darth Vader'),('Wie heißt das Schiff in Alien?','Nostromo'),('Welcher Film gewann 2004 den Oscar als bester Film?','Der Herr der Ringe: Die Rückkehr des Königs')])
add('Internet',[
('Wofür steht WWW?','World Wide Web'),('Wie nennt man ein geheimes Passwort?','Passwort'),('Welche Taste löscht meist links vom Cursor?','Backspace'),('Wie heißt ein Bild im Internet oft?','Meme'),('Was bedeutet „online“?','Mit dem Internet verbunden')],[
('Wofür steht URL?','Uniform Resource Locator'),('Welche Sprache strukturiert Webseiten?','HTML'),('Was schützt eine HTTPS-Verbindung?','Verschlüsselung'),('Wie heißt ein unerwünschtes Massen-E-Mail?','Spam'),('Was ist ein Browser?','Programm zum Öffnen von Webseiten')])
add('Essen & Trinken',[
('Aus welcher Frucht wird Guacamole gemacht?','Avocado'),('Was ist die Hauptzutat von Pommes?','Kartoffeln'),('Welches Getränk entsteht aus gerösteten Bohnen?','Kaffee'),('Welche Farbe hat eine reife Banane meist?','Gelb'),('Woraus besteht Nudelteig oft?','Mehl und Wasser')],[
('Welches Gewürz färbt Curry oft gelb?','Kurkuma'),('Wie heißt die japanische Zubereitung aus gesäuertem Reis?','Sushi'),('Welcher Käse gehört klassisch in Tiramisu?','Mascarpone'),('Wie nennt man das Garen von Gemüse in wenig Flüssigkeit?','Dünsten'),('Was ist Miso?','Fermentierte Sojabohnenpaste')])
add('Tiere & Natur',[
('Welches Tier ist das größte an Land?','Afrikanischer Elefant'),('Wie viele Beine hat eine Spinne?','8'),('Welches Tier baut Dämme?','Biber'),('Wie nennt man einen jungen Hund?','Welpe'),('Welcher Vogel kann nicht fliegen und lebt in der Antarktis?','Pinguin')],[
('Welches Säugetier kann aktiv fliegen?','Fledermaus'),('Wie heißt die Wissenschaft von Pilzen?','Mykologie'),('Welcher Baum liefert Eicheln?','Eiche'),('Wie nennt man Tiere, die nur Pflanzen fressen?','Herbivoren'),('Welches Tier hat den längsten Hals?','Giraffe')])
add('Zahlen & Logik',[
('Wie viel ist 7 mal 8?','56'),('Wie viele Minuten sind zwei Stunden?','120'),('Welche Zahl folgt auf 99?','100'),('Wie viel ist die Hälfte von 50?','25'),('Wie viele Ecken hat ein Würfel?','8')],[
('Welche Primzahl folgt auf 29?','31'),('Wie viel ist 15 Prozent von 200?','30'),('Wie viele Diagonalen hat ein Fünfeck?','5'),('Wie lautet die römische Zahl für 50?','L'),('Wie viel ist 2 hoch 10?','1024')])
add('Berühmte Menschen',[
('Wer malte die Mona Lisa?','Leonardo da Vinci'),('Wer entwickelte die Relativitätstheorie?','Albert Einstein'),('Wer schrieb „Pippi Langstrumpf“?','Astrid Lindgren'),('Wer war die erste Frau im All?','Walentina Tereschkowa'),('Wer hielt die Rede „I have a dream“?','Martin Luther King Jr.')],[
('Wer entdeckte Penicillin?','Alexander Fleming'),('Wer malte „Guernica“?','Pablo Picasso'),('Wer schrieb „1984“?','George Orwell'),('Wer war die erste Person auf dem Mond?','Neil Armstrong'),('Wer komponierte „Die Zauberflöte“?','Wolfgang Amadeus Mozart')])

# Keep exactly five bonus categories beyond the twelve requested base categories.
jeopardy=[]
C.pop('Berühmte Menschen')
for category,(r1,r2) in C.items():
    for round_no, items in ((1,r1),(2,r2)):
        for index, item in enumerate(items):
            clue, answer, *image = item
            jeopardy.append({
                'board':'Testlauf', 'boardRound':round_no, 'category':category,
                'value':(index+1)*100, 'clue':clue, 'answer':answer,
                'acceptedAnswers':'', 'hostNotes':'Host darf sinngleiche Antworten gelten lassen.',
                'imageUrl':image[0] if image else ''
            })
assert len(jeopardy)==170
json.dump(jeopardy, open(out/'jeopardy-170-testlauf-kategorien.json','w',encoding='utf8'), ensure_ascii=False, indent=2)
print(f'created words={len(words)}, wwm={len(wwm)}, jeopardy={len(jeopardy)}')

